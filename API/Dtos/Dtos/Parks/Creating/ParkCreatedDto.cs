namespace Dtos.Parks.Creating
{
    public class ParkCreatedDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? CountryCode { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsVisible { get; set; } = false;
    }
}