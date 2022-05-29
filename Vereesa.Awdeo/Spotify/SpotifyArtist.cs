using System.Text.Json.Serialization;

namespace Vereesa.Awdeo.Spotify
{
	public class SpotifyArtist
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }
	}
}