using System.Collections.Generic;
using Common.General.Localization;

namespace Dtos.ParkItems
{
    public class AttractionAccessConditionDto
    {
        public AttractionAccessConditionTypeDto Type { get; set; }
        public bool? IsCustom { get; set; }
        public double? Value { get; set; }
        public AttractionAccessConditionUnitDto? Unit { get; set; }
        public bool? RequiresAccompaniment { get; set; }
        public int? MinimumCompanionAge { get; set; }
        public List<LocalizedItem<string>>? Label { get; set; }
        public List<LocalizedItem<string>>? Description { get; set; }
        public int? DisplayOrder { get; set; }
    }
}
