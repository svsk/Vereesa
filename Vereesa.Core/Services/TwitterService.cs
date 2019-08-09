using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Newtonsoft.Json;
using RestSharp;
using Vereesa.Core.Configuration;
using Vereesa.Core.Extensions;
using Vereesa.Core.Helpers;
using Vereesa.Core.Integrations.Interfaces;

namespace Vereesa.Core.Services
{
    public class TwitterService
    {
        private TwitterSettings _settings;
        private IDiscordSocketClient _discord;
        private Timer _checkInterval;
        private long? _lastTweetIDSeen;

        public TwitterService(TwitterSettings settings, IDiscordSocketClient discord)
        {
            _settings = settings;
            _discord = discord;
            _discord.Ready += InitializeService;
        }

        private async Task InitializeService()
        {
            _checkInterval = TimerHelpers.SetTimeout(() => { CheckForNewTweets(); }, _settings.CheckIntervalSeconds * 1000, true, true);
        }

        private void CheckForNewTweets()
        {
            var latestTweets = GetLatestTweets();
            var lastTweet = latestTweets.FirstOrDefault();

            if (_lastTweetIDSeen != null && _lastTweetIDSeen != lastTweet?.Id)
            {
                SendTweetToTargetChannel(lastTweet);
            }

            _lastTweetIDSeen = lastTweet != null ? lastTweet.Id : -1;
        }

        public void SendTweetToTargetChannel(Tweet tweet)
        {
            var channel = _discord.GetGuildChannelByName(_settings.TargetDiscordGuild, _settings.TargetDiscordChannel);
            var embed = BuildEmbed(tweet);
            channel.SendMessageAsync(string.Empty, embed: embed);
        }

        public Embed BuildEmbed(Tweet tweet)
        {
            var builder = new EmbedBuilder();

            builder.WithAuthor($"@{tweet.User.ScreenName}", tweet.User.ProfileImageUrlHttps, tweet.User.Url);
            builder.WithDescription(tweet.FullText);

            if (tweet?.Entities?.Media != null && tweet.Entities.Media.Any())
                builder.WithImageUrl(tweet.Entities.Media.First().MediaUrlHttps);

            builder.WithFooter(tweet.CreatedAt);

            return builder.Build();
        }

        private string GetToken()
        {
            var restClient = new RestClient("https://api.twitter.com");
            var request = new RestRequest("/oauth2/token", Method.POST);

            request.AddHeader("Authorization", $"Basic {GetEncodedCredentials()}");
            request.AddParameter("grant_type", "client_credentials");

            var response = restClient.Execute(request);
            var result = JsonConvert.DeserializeObject<dynamic>(response.Content);

            return result["access_token"];
        }

        private string GetEncodedCredentials()
        {
            var clientId = _settings.ClientId;
            var clientSecret = _settings.ClientSecret;
            return Convert.ToBase64String(new UTF8Encoding().GetBytes(clientId + ":" + clientSecret));
        }

        public IList<Tweet> GetLatestTweets()
        {
            var token = GetToken();

            var client = new RestClient("https://api.twitter.com");
            var request = new RestRequest("/1.1/statuses/user_timeline.json", Method.GET);
            request.AddQueryParameter("screen_name", _settings.SourceTwitterUser);
            request.AddQueryParameter("count", "3");
            request.AddQueryParameter("tweet_mode", "extended");
            request.AddQueryParameter("exclude_replies", "true");
            request.AddQueryParameter("include_rts", "false");

            request.AddHeader("Authorization", $"Bearer {token}");

            var response = client.Execute(request);

            var tweets = JsonConvert.DeserializeObject<List<Tweet>>(response.Content);

            return tweets;
        }
    }



    public class Tweet
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("full_text")]
        public string FullText { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("entities")]
        public TweetEntities Entities { get; set; }

        [JsonProperty("user")]
        public TwitterUser User { get; set; }

        [JsonProperty("in_reply_to_user_id")]
        public long? InReplyToUserId { get; set; }

        [JsonProperty("retweeted_status")]
        public object RetweetedStatus { get; set; }

        [JsonIgnore]
        public bool IsRetweet => RetweetedStatus != null;

        [JsonIgnore]
        public bool IsReply => InReplyToUserId != null;
    }

    public class TweetEntities
    {
        [JsonProperty("media")]
        public List<TweetMedia> Media { get; set; }
    }

    public class TweetMedia
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("media_url_https")]
        public string MediaUrlHttps { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class TwitterUser
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("screen_name")]
        public string ScreenName { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("profile_image_url_https")]
        public string ProfileImageUrlHttps { get; set; }

        [JsonProperty("profile_link_color")]
        public string ProfileLinkColor { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}