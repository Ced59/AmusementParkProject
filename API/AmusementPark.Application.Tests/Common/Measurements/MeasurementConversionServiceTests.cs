using AmusementPark.Application.Common.Measurements;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Application.Tests.Common.Measurements;

public sealed class MeasurementConversionServiceTests
{
    [Fact]
    public void NormalizeAttractionDetails_WhenImperialValuesAreProvided_ShouldPopulateMetricTruth()
    {
        AttractionDetails details = new AttractionDetails
        {
            HeightInFeet = 200d,
            LengthInFeet = 5000d,
            SpeedInMph = 75d,
            DropInFeet = 180d,
            AccessConditions = new List<AttractionAccessCondition>
            {
                new AttractionAccessCondition
                {
                    Type = AttractionAccessConditionType.MinHeight,
                    Value = 48d,
                    Unit = AttractionAccessConditionUnit.Inch,
                },
            },
        };

        MeasurementConversionService.Instance.NormalizeAttractionDetails(details);

        Assert.Equal(60.96d, details.HeightInMeters);
        Assert.Equal(1524d, details.LengthInMeters);
        Assert.Equal(120.7d, details.SpeedInKmH);
        Assert.Equal(54.86d, details.DropInMeters);
        AttractionAccessCondition condition = Assert.Single(details.AccessConditions);
        Assert.Equal(121.92d, condition.Value);
        Assert.Equal(AttractionAccessConditionUnit.Centimeter, condition.Unit);
    }

    [Fact]
    public void NormalizeAttractionDetails_WhenMetricValuesAreProvided_ShouldKeepMetricAndDeriveImperial()
    {
        AttractionDetails details = new AttractionDetails
        {
            HeightInMeters = 61d,
            LengthInMeters = 1524d,
            SpeedInKmH = 120d,
            DropInMeters = 55d,
        };

        MeasurementConversionService.Instance.NormalizeAttractionDetails(details);

        Assert.Equal(61d, details.HeightInMeters);
        Assert.Equal(200.13d, details.HeightInFeet);
        Assert.Equal(1524d, details.LengthInMeters);
        Assert.Equal(5000d, details.LengthInFeet);
        Assert.Equal(120d, details.SpeedInKmH);
        Assert.Equal(74.56d, details.SpeedInMph);
        Assert.Equal(55d, details.DropInMeters);
        Assert.Equal(180.45d, details.DropInFeet);
    }
}
