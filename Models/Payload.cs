using Newtonsoft.Json;
using WatchParty.WS.Entities;

namespace WatchParty.WS.Models
{
    public class Payload
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "data")]
        public string Data { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "response")]
        public string Response { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "currentVideoPlaybackTime")]
        public double CurrentVideoPlaybackTime { get; set; }

        [JsonProperty(PropertyName = "isVideoPlaying")]
        public bool? IsVideoPlaying { get; set; }

        [JsonProperty(PropertyName = "date")]
        public DateTime Date => DateTime.Now;

        [JsonProperty(PropertyName = "from")]
        public string? From { get; set; }

        [JsonProperty(PropertyName = "users")]
        public IEnumerable<User>? Users { get; set; }

        [JsonProperty(PropertyName = "videos")]
        public Queue<string>? Videos { get; set; }
    }
}
