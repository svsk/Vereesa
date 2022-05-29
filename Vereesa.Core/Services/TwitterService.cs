using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Vereesa.Core.Configuration;
using Vereesa.Core.Extensions;
using Vereesa.Core.Helpers;
using Vereesa.Core.Infrastructure;
using Vereesa.Core.Integrations;

namespace Vereesa.Core.Services
{
	public class TwitterService : BotServiceBase
	{
		private readonly TwitterServiceSettings _settings;
		private readonly DiscordSocketClient _discord;
		private readonly TwitterClient _twitter;
		private readonly ILogger<TwitterService> _logger;
		private Timer _checkInterval;
		private long? _lastTweetIDSeen;

		public TwitterService(
			TwitterServiceSettings settings,
			TwitterClient twitterClient,
			DiscordSocketClient discord,
			ILogger<TwitterService> logger
		)
			: base(discord)
		{
			_settings = settings;
			_discord = discord;
			_twitter = twitterClient;

			_discord.Ready -= InitializeServiceAsync;
			_discord.Ready += InitializeServiceAsync;
			_logger = logger;
		}

		private async Task InitializeServiceAsync()
		{
			_checkInterval?.Stop();
			_checkInterval?.Dispose();
			_checkInterval = await TimerHelpers.SetTimeoutAsync(
				async () =>
				{
					await CheckForNewTweetsAsync();
				}, _settings.CheckIntervalSeconds * 1000, true, true
			);
		}

		private async Task CheckForNewTweetsAsync()
		{
			try
			{
				var latestTweets = await _twitter.GetLatestTweetsAsync(_settings.SourceTwitterUser);
				var lastTweet = latestTweets.FirstOrDefault();

				if (_lastTweetIDSeen != null && _lastTweetIDSeen != lastTweet?.Id)
				{
					await SendTweetToTargetChannelAsync(lastTweet);
				}

				_lastTweetIDSeen = lastTweet != null ? lastTweet.Id : -1;
			}
			catch (AccessViolationException)
			{
				_logger.LogWarning("Could not check for new Tweets. Failed to authenticate with Twitter.");
			}
		}

		protected virtual async Task SendTweetToTargetChannelAsync(Tweet tweet)
		{
			var channel = await _discord.GetGuildChannelByNameAsync(_settings.TargetDiscordGuild, _settings.TargetDiscordChannel);
			var embed = BuildEmbed(tweet);
			await channel.SendMessageAsync(string.Empty, embed: embed);
		}

		private Embed BuildEmbed(Tweet tweet)
		{
			var builder = new EmbedBuilder();

			builder.WithAuthor($"@{tweet.User.ScreenName}", tweet.User.ProfileImageUrlHttps, tweet.User.Url);
			builder.WithDescription(tweet.FullText);

			if (tweet?.Entities?.Media != null && tweet.Entities.Media.Any())
				builder.WithImageUrl(tweet.Entities.Media.First().MediaUrlHttps);

			builder.WithFooter(tweet.CreatedAt);

			return builder.Build();
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
