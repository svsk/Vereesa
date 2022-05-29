using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vereesa.Awdeo.Spotify;
using Vereesa.Awdeo.YouTube;

namespace Vereesa.Awdeo
{
	public static class IServiceCollectionExtensions
	{
		public static IServiceCollection AddAwdeo(this IServiceCollection services, IConfigurationRoot config)
		{
			var spotifySettings = new SpotifyClientSettings();
			config.GetSection(nameof(SpotifyClientSettings)).Bind(spotifySettings);
			services.AddSingleton(spotifySettings);
			services.AddTransient<SpotifyClient>();

			var youtubeSettings = new YouTubeClientSettings();
			config.GetSection(nameof(YouTubeClientSettings)).Bind(youtubeSettings);
			services.AddSingleton(youtubeSettings);
			services.AddTransient<YouTubeClient>();

			services.AddSingleton<AwdeoService>(); // Find a way to exclude this.

			return services;
		}
	}
}