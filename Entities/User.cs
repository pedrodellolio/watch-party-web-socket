using Newtonsoft.Json;
using System.Net.WebSockets;

namespace WatchParty.WS.Entities
{
    public class User
    {
        [JsonIgnore]
        public Guid Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "avatarUrl")]
        public string AvatarUrl { get; set; } = string.Empty;

        [JsonIgnore]
        public required WebSocket WebSocket { get; set; }
    }
}
