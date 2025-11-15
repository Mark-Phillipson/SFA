using System.Text.Json.Serialization;

namespace SFA_PWA.Models
{
    public class DeviceInfo
    {
        [JsonPropertyName("os")]
        public string OS { get; set; } = "Unknown";

        [JsonPropertyName("isMobile")]
        public bool IsMobile { get; set; }

        [JsonPropertyName("browser")]
        public string Browser { get; set; } = "Unknown";

        [JsonPropertyName("ua")]
        public string UserAgent { get; set; } = string.Empty;
    }
}
