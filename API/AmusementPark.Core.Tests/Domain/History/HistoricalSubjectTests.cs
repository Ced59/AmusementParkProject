using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalSubjectTests
{
    [Fact]
    public void Constructor_ShouldNormalizeStableIdentityAndFrozenLabel()
    {
        HistoricalSubject subject = new HistoricalSubject(
            HistoricalSubjectType.Park,
            " park-1 ",
            " Ancien nom du parc ",
            HistoricalSubjectPublicationPolicy.HistoricalOnly);

        Assert.Equal("park-1", subject.Id);
        Assert.Equal("Ancien nom du parc", subject.HistoricalLabel);
        Assert.Equal(HistoricalSubjectPublicationPolicy.HistoricalOnly, subject.PublicationPolicy);
    }

    [Fact]
    public void Constructor_WhenIdentifierIsMissing_ShouldRejectSubject()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => new HistoricalSubject(
                HistoricalSubjectType.Park,
                " ",
                "Parc",
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidIdentifier, exception.ErrorCode);
    }
}
