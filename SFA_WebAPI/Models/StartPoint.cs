using System.Text.Json.Serialization;

namespace SFA_WebAPI.Models
{
    public class StartPoint
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Location { get; set; }
        public string? Postcode { get; set; }
        public Coordinates? Coordinates { get; set; }
        public string? PlusCode { get; set; }
        public string? GoogleMaps { get; set; }
        [JsonPropertyName("what3words")]
        public string? What3Words { get; set; }
        public string? Notes { get; set; }
    }

    public class Coordinates
    {
        public double? Lat { get; set; }
        [JsonPropertyName("lng")]
        public double? Lng { get; set; }
    }
}
