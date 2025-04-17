using Google.Apis.YouTube.v3.Data;

namespace WatchParty.WS.Services
{
    public interface IYoutubeService
    {
        Task<Entities.Video?> GetVideoAsync(string videoUrl);
    }
}
