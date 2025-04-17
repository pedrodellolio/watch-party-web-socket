using Google.Apis.Services;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Configuration;
using WatchParty.WS.Helpers;
using System.Text.RegularExpressions;

namespace WatchParty.WS.Services
{
    public class YoutubeService : IYoutubeService
    {
        private readonly YouTubeService _youtubeService;

        public YoutubeService(IConfiguration configuration)
        {
            _youtubeService = new YouTubeService(new BaseClientService.Initializer
            {
                ApiKey = configuration["YoutubeApiKey"],
                ApplicationName = nameof(YouTubeHelper)
            });
        }

        /// <summary>
        /// Fetches metadata for a single YouTube video by its ID.
        /// </summary>
        /// <param name="videoUrl">The YouTube video URL.</param>
        /// <returns>The Video resource, or null if not found.</returns>
        public async Task<Entities.Video?> GetVideoAsync(string videoUrl)
        {
            var videoId = videoUrl.Split("?v=")[1];
            var request = _youtubeService.Videos.List("snippet,contentDetails,statistics");
            request.Id = videoId;

            var response = await request.ExecuteAsync();

            var item = response.Items.Count > 0
                ? response.Items[0]
                : null;

            return item is not null ?
                new Entities.Video
                {
                    Title = item.Snippet.Title,
                    Channel = item.Snippet.ChannelTitle,
                    Thumbnail = item.Snippet.Thumbnails.Default__.Url,
                    Duration = GetVideoDurationInSeconds(item.ContentDetails.Duration),
                    URL = videoUrl
                } : null;
        }

        private static int GetVideoDurationInSeconds(string duration)
        {
            var regex = new Regex(@"^PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?$");
            var match = regex.Match(duration);
            if (match.Success)
            {
                int hours = int.Parse(string.IsNullOrEmpty(match.Groups[1].Value) ? "0" : match.Groups[1].Value);
                int minutes = int.Parse(string.IsNullOrEmpty(match.Groups[2].Value) ? "0" : match.Groups[2].Value);
                int seconds = int.Parse(string.IsNullOrEmpty(match.Groups[3].Value) ? "0" : match.Groups[3].Value);
                return (hours * 3600) + (minutes * 60) + seconds;
            }
            return 0;
        }


        private static string FormatDuration(int hours, int minutes, int seconds)
        {
            var parts = new List<string>();

            if (hours > 0)
                parts.Add(hours.ToString("D2"));
            if (minutes > 0)
                parts.Add(minutes.ToString("D2"));

            parts.Add(seconds.ToString("D2"));

            return string.Join(":", parts);
        }
    }
}
