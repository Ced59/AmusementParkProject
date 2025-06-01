namespace WebAPI.Resources.InitializingDatas
{
    public class ParkJson
    {
        public int Id { get; set; }
        public CountryJson? Country { get; set; }
        public string? Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
