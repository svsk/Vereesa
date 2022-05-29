using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Vereesa.Awdeo.Spotify
{
	public class SpotifyPlaylistTrack
	{
		[JsonPropertyName("track")]
		public SpotifyTrack Track { get; set; }
	}

	public class SpotifyTrack
	{
		[JsonPropertyName("id")]
		public string Id { get; set; }

		[JsonPropertyName("duration_ms")]
		public ulong DurationMs { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("artists")]
		public List<SpotifyArtist> Artists { get; set; }
	}
}