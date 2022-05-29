using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Vereesa.Awdeo.Spotify
{
	public class SpotifyPlaylist
	{
		[JsonPropertyName("items")]
		public List<SpotifyPlaylistTrack> Items { get; set; }

		[JsonPropertyName("total")]
		public int Total { get; set; }

		[JsonPropertyName("limit")]
		public int Taken { get; set; }

		[JsonPropertyName("offset")]
		public int Skipped { get; set; }
	}
}