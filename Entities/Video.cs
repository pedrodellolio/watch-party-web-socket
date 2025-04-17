using Newtonsoft.Json;
using System.Net.WebSockets;

namespace WatchParty.WS.Entities
{
    public class Video
    {
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "thumbnail")]
        public string Thumbnail { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "url")]
        public string URL { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "channel")]
        public string Channel { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "duration")]
        public int Duration { get; set; }

        [JsonIgnore]
        public string Description
        {
            get
            {
                return $"{Title} - {Channel}";
            }
        }
    }
}
