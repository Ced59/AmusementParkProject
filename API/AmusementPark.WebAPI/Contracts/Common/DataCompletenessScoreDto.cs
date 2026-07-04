namespace AmusementPark.WebAPI.Contracts.Common;

public sealed class DataCompletenessScoreDto
{
    public int CompletenessScore { get; set; }

    public string DataQualityLevel { get; set; } = string.Empty;

    public int ApplicableMaxPoints { get; set; }

    public int EarnedPoints { get; set; }
}
