using Common.General;

namespace Entities.Model.Attractions
{
    public class Attraction : ModelBase
    {
        public GeneralStatus OpenStatus { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string AttractionType { get; set; }
    }
}