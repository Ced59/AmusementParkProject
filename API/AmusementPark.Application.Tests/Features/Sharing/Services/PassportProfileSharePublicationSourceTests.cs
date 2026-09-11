using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Services;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class PassportProfileSharePublicationSourceTests
{
    [Fact]
    public async Task GetCurrentSourceVersionAsync_ShouldReadOnlyConstantTimeRevisionDocuments()
    {
        const string ownerUserId = "owner-1";
        string passportScope = PassportProfileShareSourceScope.Create(ownerUserId);
        string identityScope = PersonalRankingShareSourceScope.Create(ownerUserId);
        Mock<IPassportProfileSourceReader> sourceReader =
            new Mock<IPassportProfileSourceReader>(MockBehavior.Strict);
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.GetSnapshotAsync(
                It.Is<IReadOnlyCollection<string>>(scopes => scopes.Count == 3
                    && scopes.Contains(passportScope)
                    && scopes.Contains(identityScope)
                    && scopes.Contains(PersonalRankingShareSourceScope.PublicCatalog)),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
            {
                [passportScope] = Revision(4),
                [identityScope] = Revision(7),
                [PersonalRankingShareSourceScope.PublicCatalog] = Revision(3),
            });
        PassportProfileSharePublicationSource source = new PassportProfileSharePublicationSource(
            sourceReader.Object,
            revisions.Object);

        ApplicationResult<long> result = await source.GetCurrentSourceVersionAsync(
            passportScope,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(14, result.Value);
        sourceReader.VerifyNoOtherCalls();
        revisions.VerifyAll();
    }

    private static ShareSourceRevision Revision(long value)
    {
        return new ShareSourceRevision(
            value,
            0,
            new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc));
    }
}
