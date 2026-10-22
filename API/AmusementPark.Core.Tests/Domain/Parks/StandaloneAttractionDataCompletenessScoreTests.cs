using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Parks;

public sealed class StandaloneAttractionDataCompletenessScoreTests
{
    [Fact]
    public void ProjectedScore_UsesNativeFactsWithoutPublishingOrRequiringAParentOrMinimumHeight()
    {
        StandaloneCompletenessFixture fixture = new();

        DataCompletenessScore projected = fixture.Attraction.CalculateDataCompletenessScore(fixture.Context);
        DataCompletenessScore actual = fixture.Attraction.CalculateDataCompletenessScore(fixture.Context with { ProjectForPublication = false });

        Assert.Equal(100, projected.CompletenessScore);
        Assert.Null(projected.PublicationBlocker);
        Assert.True(actual.CompletenessScore < projected.CompletenessScore);
        Assert.False(fixture.Attraction.IsVisible);
        Assert.False(fixture.Context.Images.Single().IsPublished);
        Assert.False(fixture.Context.HistoryEvents.Single().IsVisible);
        Assert.False(fixture.Context.HistoryEvents.Single().Article!.IsPublished);
        Assert.False(fixture.Context.Manufacturer!.IsVisible);
    }

    [Fact]
    public void RealScore_AfterPublishingTheSameCandidate_MatchesProjection()
    {
        StandaloneCompletenessFixture fixture = new();
        DataCompletenessScore projected = fixture.Attraction.CalculateDataCompletenessScore(fixture.Context);
        fixture.Attraction.IsVisible = true;
        fixture.Context.Images.Single().IsPublished = true;
        fixture.Context.HistoryEvents.Single().IsVisible = true;
        fixture.Context.HistoryEvents.Single().Article!.IsPublished = true;
        fixture.Context.Manufacturer!.IsVisible = true;

        Assert.Equal(projected, fixture.Attraction.CalculateDataCompletenessScore(fixture.Context with { ProjectForPublication = false }));
    }

    [Fact]
    public void ForeignOwnersAndUnresolvedReferences_DoNotContributeOrLeakTheirText()
    {
        StandaloneCompletenessFixture fixture = new();
        fixture.Context.Images.Single().OwnerId = "another-attraction";
        fixture.Context.Images.Single().Description = "public page";
        fixture.Context.HistoryEvents.Single().OwnerId = "another-attraction";
        fixture.Context.Operator!.Id = "another-operator";
        fixture.Context.Manufacturer!.Id = "another-manufacturer";
        fixture.Context.Pricing!.ParkId = "another-attraction";
        fixture.Context.OpeningHours!.ParkId = "another-attraction";

        DataCompletenessScore result = fixture.Attraction.CalculateDataCompletenessScore(fixture.Context);

        Assert.True(result.CompletenessScore < 95);
        Assert.Equal("references.unresolved", result.PublicationBlocker);
    }

    [Theory]
    [InlineData("ClosedDefinitively")]
    [InlineData("Planned")]
    public void NonOperatingAttraction_DoesNotRequireCurrentVisitorInformation(string status)
    {
        StandaloneCompletenessFixture fixture = new();
        fixture.Attraction.AttractionDetails!.Status = status;
        fixture.Attraction.AttractionDetails.ClosingDateText = "2025";
        fixture.Context.Pricing!.Notes = StandaloneCompletenessFixture.Texts("public page");
        fixture.Context.OpeningHours!.Notes = "public page";

        DataCompletenessScore result = fixture.Attraction.CalculateDataCompletenessScore(fixture.Context);

        Assert.Equal(100, result.CompletenessScore);
        Assert.Null(result.PublicationBlocker);
        Assert.Equal(result, fixture.Attraction.CalculateDataCompletenessScore(fixture.Context with { Pricing = null, OpeningHours = null }));
    }

    [Fact]
    public void ExpiredPricesAndSchedule_AndFutureVerification_DoNotCountAsCurrent()
    {
        StandaloneCompletenessFixture fixture = new();
        fixture.Context.Pricing!.AdmissionOffers[0].ValidTo = new(2026, 10, 21);
        fixture.Context.OpeningHours!.RegularRules[0].EndDate = new(2026, 10, 21);
        fixture.Context.Pricing.LastVerifiedAtUtc = StandaloneCompletenessFixture.AsOf.AddDays(1);
        fixture.Context.OpeningHours.LastVerifiedAtUtc = StandaloneCompletenessFixture.AsOf.AddDays(1);

        Assert.True(fixture.Attraction.CalculateDataCompletenessScore(fixture.Context).CompletenessScore < 95);
    }

    [Fact]
    public void CalendarRuleWithNoRemainingMatchingWeekday_DoesNotCountAsFutureCoverage()
    {
        StandaloneCompletenessFixture fixture = new();
        fixture.Context.OpeningHours!.RegularRules[0].EndDate = new(2026, 10, 22);
        fixture.Context.OpeningHours.RegularRules[0].DaysOfWeek = [DayOfWeek.Wednesday];
        DataCompletenessScore noRemainingDay = fixture.Attraction.CalculateDataCompletenessScore(fixture.Context);
        fixture.Context.OpeningHours.RegularRules[0].DaysOfWeek = [DayOfWeek.Thursday];

        Assert.True(noRemainingDay.CompletenessScore < fixture.Attraction.CalculateDataCompletenessScore(fixture.Context).CompletenessScore);
    }

    [Fact]
    public void InvalidEmptyPrice_DoesNotReceiveCredit_ButExplicitFreeAdmissionDoes()
    {
        StandaloneCompletenessFixture fixture = new();
        fixture.Context.Pricing!.AdmissionOffers[0].OnlinePrice = new();
        DataCompletenessScore invalidPrice = fixture.Attraction.CalculateDataCompletenessScore(fixture.Context);
        fixture.Context.Pricing.AdmissionOffers[0].OnlinePrice!.Amount = 0;

        Assert.True(invalidPrice.CompletenessScore < 95);
        Assert.Equal(100, fixture.Attraction.CalculateDataCompletenessScore(fixture.Context).CompletenessScore);
    }

    [Theory]
    [InlineData("description")]
    [InlineData("image")]
    [InlineData("history")]
    [InlineData("article")]
    [InlineData("operator")]
    [InlineData("manufacturer")]
    [InlineData("access-source")]
    [InlineData("price")]
    [InlineData("hours")]
    [InlineData("reference-image")]
    public void ForbiddenText_InAnyPublicSurface_BlocksPublication(string surface)
    {
        StandaloneCompletenessFixture fixture = new();
        List<LocalizedText> bad = StandaloneCompletenessFixture.Texts("This public page contains the current inventory.");
        switch (surface)
        {
            case "description": fixture.Attraction.Descriptions = bad; break;
            case "image": fixture.Context.Images.Single().AltTexts = bad; break;
            case "history": fixture.Context.HistoryEvents.Single().Summaries = bad; break;
            case "article": fixture.Context.HistoryEvents.Single().Article!.Blocks[1].Texts = bad; break;
            case "operator": fixture.Context.Operator!.Description = bad; break;
            case "manufacturer": fixture.Context.Manufacturer!.Biography = bad; break;
            case "access-source": fixture.Attraction.AttractionDetails!.AccessConditions[0].SourceSummary = bad; break;
            case "price": fixture.Context.Pricing!.AdmissionOffers[0].Conditions = bad; break;
            case "hours": fixture.Context.OpeningHours!.Notes = bad[0].Value; break;
            case "reference-image": fixture.Context = fixture.Context with { ReferenceImages = [new Image
                { OwnerType = ImageOwnerType.ParkOperator, OwnerId = "operator-1", Captions = bad }] }; break;
        }

        DataCompletenessScore result = fixture.Attraction.CalculateDataCompletenessScore(fixture.Context);

        Assert.True(result.CompletenessScore <= 95);
        Assert.Equal(DataCompletenessScoringRules.ForbiddenPublicTextPublicationBlocker, result.PublicationBlocker);
    }

    [Fact]
    public void RepeatedNarrativeAfterNameAndHeadingRemoval_BlocksWithDiagnostic()
    {
        StandaloneCompletenessFixture fixture = new();
        fixture.Attraction.Descriptions = StandaloneCompletenessFixture.Texts(
            "<h2>The headland</h2><p>Cliff Flight brings local tales to life through murals beside the entrance.</p>");
        fixture.Context.Operator!.Description = StandaloneCompletenessFixture.Texts(
            "<h2>The company</h2><p>Coastal Leisure brings local tales to life through murals beside the entrance.</p>");

        DataCompletenessScore result = fixture.Attraction.CalculateDataCompletenessScore(fixture.Context);

        Assert.Equal(DataCompletenessScoringRules.FormulaicPublicTextPublicationBlocker, result.PublicationBlocker);
        Assert.NotNull(result.FormulaicPublicTextIssue);
        Assert.True(result.CompletenessScore <= 95);
    }

    [Fact]
    public void LegitimateRepeatedStructuredConditions_AreNotNarrativeBoilerplate()
    {
        StandaloneCompletenessFixture fixture = new();
        List<LocalizedText> conditions = StandaloneCompletenessFixture.Texts("The voucher remains valid for twelve months after the date of purchase.");
        fixture.Context.Pricing!.AdmissionOffers[0].Conditions = conditions;
        fixture.Context.Pricing.AdmissionOffers.Add(new() { Labels = StandaloneCompletenessFixture.Texts("Child"),
            OnlinePrice = new() { Amount = 10 }, Conditions = conditions });

        Assert.Equal(100, fixture.Attraction.CalculateDataCompletenessScore(fixture.Context).CompletenessScore);
    }

    [Fact]
    public void ExcludedAttractionAndDanglingArticlePhoto_CannotPassPublicationThreshold()
    {
        StandaloneCompletenessFixture fixture = new();
        fixture.Attraction.AdminReviewStatus = AdminReviewStatus.NotRelevant;
        Assert.Equal("standalone-attraction.not-relevant", fixture.Attraction.CalculateDataCompletenessScore(fixture.Context).PublicationBlocker);
        fixture.Attraction.AdminReviewStatus = AdminReviewStatus.ToReview;
        fixture.Context.HistoryEvents.Single().Article!.MainImageId = "unrelated-photo";
        DataCompletenessScore result = fixture.Attraction.CalculateDataCompletenessScore(fixture.Context);
        Assert.Equal("media.unresolved-reference", result.PublicationBlocker);
        Assert.True(result.CompletenessScore <= 95);
    }

    [Fact]
    public void ArticleMayUseLinkedReferencePhoto_ButNotAnUnrelatedReferencePhoto()
    {
        StandaloneCompletenessFixture fixture = new();
        fixture.Context.HistoryEvents.Single().Article!.MainImageId = "operator-photo";
        Image referencePhoto = new() { Id = "operator-photo", OwnerType = ImageOwnerType.ParkOperator, OwnerId = "operator-1" };
        fixture.Context = fixture.Context with { ReferenceImages = [referencePhoto] };
        Assert.Null(fixture.Attraction.CalculateDataCompletenessScore(fixture.Context).PublicationBlocker);
        referencePhoto.OwnerId = "another-operator";
        Assert.Equal("media.unresolved-reference", fixture.Attraction.CalculateDataCompletenessScore(fixture.Context).PublicationBlocker);
    }
}
