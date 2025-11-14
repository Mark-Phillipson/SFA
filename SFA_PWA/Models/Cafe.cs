namespace SFA_PWA.Models
{
    public class Cafe
    {
        public string? ToRWGPS { get; set; }
        public string? Status { get; set; }
        public string? Name { get; set; }
        public string? LatLong { get; set; }
        public string? PlusCode { get; set; }
        public string? GoogleMapLink { get; set; }
        public string? CafeMapLink { get; set; }
        public string? Description { get; set; }
        public string? Place { get; set; }
        public string? Address { get; set; }
        public string? Postcode { get; set; }
        public string? Tel { get; set; }
        public string? OSGrid { get; set; }
        public string? Notes { get; set; }

        // For filtering/location
        public string? Location { get; set; }
        public double? Distance { get; set; }
    }
}
