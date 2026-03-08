namespace Dtos.ParkZones
{
    public class ParkExplorerBucketDto
    {
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public bool IsVirtual { get; set; }
        public int TotalItems { get; set; }
        public List<ParkZoneSummaryCountDto> CountsByCategory { get; set; } = new();
        public List<ParkZoneSummaryCountDto> CountsByType { get; set; } = new();
    }
}
