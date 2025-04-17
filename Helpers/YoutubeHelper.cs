using System;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace WatchParty.WS.Helpers
{
    public class YouTubeHelper
    {
        private readonly YouTubeService _youtubeService;
        private readonly IConfiguration _configuration;
        public YouTubeHelper(IConfiguration configuration)
        {
            _configuration = configuration;
            _youtubeService = new YouTubeService(new BaseClientService.Initializer
            {
                ApiKey = _configuration["AppSettings:ApiKey:Youtube"],
                ApplicationName = nameof(YouTubeHelper)
            });
        }

        /// <summary>
        /// Fetches metadata for a single YouTube video by its ID.
        /// </summary>
        /// <param name="videoUrl">The YouTube video URL.</param>
        /// <returns>The Video resource, or null if not found.</returns>
        public async Task<Video?> GetVideoAsync(string videoUrl)
        {
            var videoId = videoUrl.Split("?v")[1];
            var request = _youtubeService.Videos.List("snippet,contentDetails,statistics");
            request.Id = videoId;

            var response = await request.ExecuteAsync();
            return response.Items.Count > 0
                ? response.Items[0]
                : null;
        }
    }
}