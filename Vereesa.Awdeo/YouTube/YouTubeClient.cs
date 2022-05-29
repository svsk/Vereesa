using System.Collections.Generic;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace Vereesa.Awdeo.YouTube
{
	public class YouTubeClient
	{
		private YouTubeClientSettings _settings;

		public YouTubeClient(YouTubeClientSettings settings)
		{
			_settings = settings;
		}

		public List<ExternalVideo> SearchForVideo(string name, ulong duration, string artist)
		{
			var result = new List<ExternalVideo>();

			var youtubeService = new YouTubeService(new BaseClientService.Initializer()
			{
				ApiKey = _settings.ApiKey,
				ApplicationName = _settings.ApplicationName
			});

			var searchRequest = youtubeService.Search.List("snippet");
			searchRequest.Q = name + " " + artist;
			searchRequest.Type = "video";
			searchRequest.MaxResults = 5;

			var response = searchRequest.Execute();

			foreach (var searchResult in response.Items)
			{
				if (searchResult.Id.Kind == "youtube#video")
				{
					result.Add(new ExternalVideo
					{
						Title = searchResult.Snippet.Title,
						ExternalSystemRef = searchResult.Id.VideoId,
						ExternalSystemId = "YouTube"
					});
				}
			}

			return result;
		}
	}

	public class ExternalVideo
	{
		public string Title { get; set; }
		public int Duration { get; set; }
		public string ExternalSystemRef { get; set; }
		public string ExternalSystemId { get; set; }
		public string ResourceLocation { get; set; }
	}
}