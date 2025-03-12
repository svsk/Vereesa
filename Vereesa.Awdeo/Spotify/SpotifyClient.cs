using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Timers;
using RestSharp;

namespace Vereesa.Awdeo.Spotify
{
    public class SpotifyClient
    {
        private static string _authKey;
        private static Timer _authKeyTimer;
        private SpotifyClientSettings _settings;

        public SpotifyClient(SpotifyClientSettings settings)
        {
            _settings = settings;
        }

        private void Authorize()
        {
            if (_authKey != null)
                return;

            var credentials = _settings.Credentials;

            var client = new RestClient("https://accounts.spotify.com");
            var request = new RestRequest("/api/token", Method.Post);

            request.AddHeader("Authorization", $"Basic {credentials}");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("grant_type", "client_credentials");

            var response = client.Execute<Dictionary<string, object>>(request);
            var accessToken = response.Data["access_token"] as string;
            var expiration = int.Parse(response.Data["expires_in"].ToString());

            _authKey = accessToken;
            _authKeyTimer = new Timer(expiration * 1000);
            _authKeyTimer.Elapsed += HandleAuthExpired;
            _authKeyTimer.Start();
        }

        private void HandleAuthExpired(object sender, ElapsedEventArgs e)
        {
            _authKey = null;
        }

        public List<SpotifyTrack> GetPlaylistTracks(string playlistId)
        {
            var take = 100;
            var taken = 0;
            var playlistLength = int.MaxValue;

            var tracks = new List<SpotifyTrack>();
            while (taken < playlistLength)
            {
                var playlist = GetSpotifyPlaylist(playlistId, taken, take);
                playlistLength = playlist.Total;
                tracks.AddRange(playlist.Items.Select(t => t.Track));
                taken += take;
            }

            return tracks;
        }

        private SpotifyPlaylist GetSpotifyPlaylist(string playlistId, int skip = 0, int take = 100)
        {
            playlistId = EnsureCleanId(playlistId);
            Authorize();

            var client = new RestClient();
            var request = new RestRequest($"https://api.spotify.com/v1/playlists/{playlistId}/tracks", Method.Get);

            request.AddQueryParameter("market", "NO");
            request.AddQueryParameter("fields", "total,offset,limit,items(track(id,name,duration_ms,artists(name)))");
            request.AddQueryParameter("offset", $"{skip}");
            request.AddQueryParameter("limit", $"{take}");

            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", $"Bearer {_authKey}");

            var response = client.Execute(request);

            var playlist = JsonSerializer.Deserialize<SpotifyPlaylist>(response.Content);
            return playlist;
        }

        private string EnsureCleanId(string id)
        {
            id = id.Split("/").Last();
            id = id.Split("?").First();
            return id;
        }

        public SpotifyTrack GetTrack(string trackId)
        {
            trackId = EnsureCleanId(trackId);
            Authorize();

            var client = new RestClient();
            var request = new RestRequest($"https://api.spotify.com/v1/tracks/{trackId}", Method.Get);

            request.AddQueryParameter("market", "NO");
            //request.AddQueryParameter("fields", "total,offset,limit,items(track(name,duration_ms,artists(name)))");

            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", $"Bearer {_authKey}");

            var response = client.Execute(request);
            var track = JsonSerializer.Deserialize<SpotifyTrack>(response.Content);
            return track;
        }

        public string GetIdFromUri(string link) => EnsureCleanId(link);
    }
}
