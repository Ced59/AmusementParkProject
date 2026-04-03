namespace Dtos.Parks.Creating
{
    public class ParkCreateDto
    {
        public string? Name { get; set; }
        public string? CountryCode { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsVisible { get; set; } = false;
        public string? WebsiteUrl { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
    }
}