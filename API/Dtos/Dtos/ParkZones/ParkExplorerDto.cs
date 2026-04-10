namespace Dtos.ParkZones
{
    public class ParkExplorerDto
    {
        public string ParkId { get; set; } = string.Empty;
        public bool HasZones { get; set; }
        public ParkExplorerBucketDto Overview { get; set; } = new();
        public List<ParkExplorerBucketDto> Zones { get; set; } = new();
        public ParkExplorerBucketDto? Unassigned { get; set; }
    }
}
