namespace Dtos.ParkItems.ParkItems
{
    public class ParkItemAdminListDto
    {
        public string Id { get; set; } = string.Empty;
        public string ParkId { get; set; } = string.Empty;
        public string ParkName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public ParkItemCategoryDto Category { get; set; }
        public ParkItemTypeDto Type { get; set; }
        public bool IsVisible { get; set; }
    }
}
