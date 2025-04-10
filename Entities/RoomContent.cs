namespace WatchParty.WS.Entities
{
    public class RoomContent
    {
        public List<User> Users { get; set; } = [];
        public Queue<string> Videos { get; set; } = [];
    }
}
