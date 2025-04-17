using Google.Apis.YouTube.v3.Data;

namespace WatchParty.WS.Entities
{
    public class RoomContent
    {
        public List<User> Users { get; set; } = [];
        public Queue<Video> Videos { get; set; } = [];
        public Video? CurrentVideoPlaying { get; set; }
    }
}
