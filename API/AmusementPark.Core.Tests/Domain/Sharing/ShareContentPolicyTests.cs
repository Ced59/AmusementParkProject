using AmusementPark.Core.Domain.Sharing;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Sharing;

public sealed class ShareContentPolicyTests
{
    [Theory]
    [MemberData(nameof(AllowedFieldSets))]
    public void Create_ShouldApplyTheExhaustiveFieldWhitelist(
        SharePublicationType publicationType,
        ShareContentField[] allowedFields)
    {
        ShareContentField[] allFields = Enum.GetValues<ShareContentField>();

        foreach (ShareContentField field in allFields)
        {
            if (allowedFields.Contains(field))
            {
                ShareContentPolicy policy = ShareContentPolicy.Create(
                    publicationType,
                    ShareDatePrecision.Hidden,
                    new[] { field });

                Assert.True(policy.Includes(field));
            }
            else
            {
                ShareContentPolicyValidationException exception =
                    Assert.Throws<ShareContentPolicyValidationException>(() => ShareContentPolicy.Create(
                        publicationType,
                        ShareDatePrecision.Hidden,
                        new[] { field }));

                Assert.Equal(ShareContentPolicyErrorCodes.ContentFieldNotAllowed, exception.ErrorCode);
            }
        }
    }

    [Fact]
    public void AllowedFieldSets_ShouldCoverEveryPublicationTypeExactlyOnce()
    {
        object[][] cases = AllowedFieldSets().ToArray();
        SharePublicationType[] coveredTypes = cases
            .Select(static values => (SharePublicationType)values[0])
            .ToArray();

        Assert.Equal(Enum.GetValues<SharePublicationType>().Length, cases.Length);
        Assert.Equal(coveredTypes.Length, coveredTypes.Distinct().Count());
    }

    [Theory]
    [InlineData(SharePublicationType.VisitRecap, ShareDatePrecision.Day)]
    [InlineData(SharePublicationType.YearRecap, ShareDatePrecision.Year)]
    [InlineData(SharePublicationType.PassportProfile, ShareDatePrecision.Year)]
    [InlineData(SharePublicationType.PersonalRanking, ShareDatePrecision.Hidden)]
    [InlineData(SharePublicationType.ProfileComparison, ShareDatePrecision.Day)]
    public void Create_ShouldApplyTheMaximumDatePrecisionPerType(
        SharePublicationType publicationType,
        ShareDatePrecision maximumPrecision)
    {
        foreach (ShareDatePrecision precision in Enum.GetValues<ShareDatePrecision>())
        {
            if (precision <= maximumPrecision)
            {
                ShareContentPolicy policy = ShareContentPolicy.Create(
                    publicationType,
                    precision,
                    Array.Empty<ShareContentField>());

                Assert.Equal(precision, policy.DatePrecision);
            }
            else
            {
                ShareContentPolicyValidationException exception =
                    Assert.Throws<ShareContentPolicyValidationException>(() => ShareContentPolicy.Create(
                        publicationType,
                        precision,
                        Array.Empty<ShareContentField>()));

                Assert.Equal(ShareContentPolicyErrorCodes.DatePrecisionNotAllowed, exception.ErrorCode);
            }
        }
    }

    [Theory]
    [InlineData((SharePublicationType)99, ShareContentPolicyErrorCodes.InvalidPublicationType)]
    public void Create_WhenPublicationTypeIsUnknown_ShouldRejectIt(
        SharePublicationType publicationType,
        string expectedErrorCode)
    {
        ShareContentPolicyValidationException exception =
            Assert.Throws<ShareContentPolicyValidationException>(() => ShareContentPolicy.CreatePrivateDefault(
                publicationType));

        Assert.Equal(expectedErrorCode, exception.ErrorCode);
    }

    [Fact]
    public void Create_WhenDatePrecisionIsUnknown_ShouldRejectIt()
    {
        ShareContentPolicyValidationException exception =
            Assert.Throws<ShareContentPolicyValidationException>(() => ShareContentPolicy.Create(
                SharePublicationType.VisitRecap,
                (ShareDatePrecision)99,
                Array.Empty<ShareContentField>()));

        Assert.Equal(ShareContentPolicyErrorCodes.InvalidDatePrecision, exception.ErrorCode);
    }

    [Fact]
    public void Create_WhenContentFieldIsUnknown_ShouldRejectIt()
    {
        ShareContentPolicyValidationException exception =
            Assert.Throws<ShareContentPolicyValidationException>(() => ShareContentPolicy.Create(
                SharePublicationType.VisitRecap,
                ShareDatePrecision.Hidden,
                new[] { (ShareContentField)99 }));

        Assert.Equal(ShareContentPolicyErrorCodes.InvalidContentField, exception.ErrorCode);
    }

    [Fact]
    public void CreatePrivateDefault_ShouldExposeNothing()
    {
        ShareContentPolicy policy = ShareContentPolicy.CreatePrivateDefault(
            SharePublicationType.PassportProfile);

        Assert.Equal(ShareContentPolicy.CurrentSchemaVersion, policy.SchemaVersion);
        Assert.Equal(ShareDatePrecision.Hidden, policy.DatePrecision);
        Assert.Empty(policy.IncludedFields);
        Assert.All(Enum.GetValues<ShareContentField>(), field => Assert.False(policy.Includes(field)));
    }

    [Fact]
    public void Create_ShouldDeduplicateSortAndProtectTheSelectedFields()
    {
        ShareContentField[] requestedFields =
        {
            ShareContentField.GlobalRatings,
            ShareContentField.PublicDisplayName,
            ShareContentField.GlobalRatings,
        };
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.PersonalRanking,
            ShareDatePrecision.Hidden,
            requestedFields);
        ICollection<ShareContentField> fields =
            Assert.IsAssignableFrom<ICollection<ShareContentField>>(policy.IncludedFields);

        Assert.Equal(
            new[] { ShareContentField.PublicDisplayName, ShareContentField.GlobalRatings },
            policy.IncludedFields);
        Assert.True(fields.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => fields.Add(ShareContentField.Avatar));
    }

    [Fact]
    public void Restore_WhenSchemaVersionIsUnsupported_ShouldRejectIt()
    {
        ShareContentPolicyValidationException exception =
            Assert.Throws<ShareContentPolicyValidationException>(() => ShareContentPolicy.Restore(
                SharePublicationType.VisitRecap,
                ShareContentPolicy.CurrentSchemaVersion + 1,
                ShareDatePrecision.Hidden,
                Array.Empty<ShareContentField>()));

        Assert.Equal(ShareContentPolicyErrorCodes.UnsupportedSchemaVersion, exception.ErrorCode);
    }

    [Fact]
    public void PublicPolicyContract_ShouldNotRepresentForbiddenPrivateCategories()
    {
        string[] forbiddenTerms =
        {
            "PrivateComment",
            "PrivateNote",
            "Latitude",
            "Longitude",
            "PreciseLocation",
            "Companion",
            "SeoIndex",
        };
        string[] publicPolicyNames = Enum.GetNames<ShareContentField>()
            .Concat(typeof(ShareContentPolicy).GetProperties().Select(static property => property.Name))
            .ToArray();

        Assert.All(
            forbiddenTerms,
            forbidden => Assert.DoesNotContain(
                publicPolicyNames,
                name => name.Contains(forbidden, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void HasSameSelectionAs_ShouldCompareTheCanonicalWhitelist()
    {
        ShareContentPolicy first = ShareContentPolicy.Create(
            SharePublicationType.VisitRecap,
            ShareDatePrecision.Month,
            new[] { ShareContentField.RideCount, ShareContentField.Avatar });
        ShareContentPolicy same = ShareContentPolicy.Create(
            SharePublicationType.VisitRecap,
            ShareDatePrecision.Month,
            new[] { ShareContentField.Avatar, ShareContentField.RideCount });
        ShareContentPolicy different = ShareContentPolicy.Create(
            SharePublicationType.VisitRecap,
            ShareDatePrecision.Year,
            new[] { ShareContentField.Avatar, ShareContentField.RideCount });

        Assert.True(first.HasSameSelectionAs(same));
        Assert.False(first.HasSameSelectionAs(different));
    }

    public static IEnumerable<object[]> AllowedFieldSets()
    {
        yield return new object[]
        {
            SharePublicationType.VisitRecap,
            new[]
            {
                ShareContentField.PublicDisplayName,
                ShareContentField.Avatar,
                ShareContentField.RideCount,
                ShareContentField.TemporalRatings,
                ShareContentField.GlobalRatings,
                ShareContentField.PublicCaption,
                ShareContentField.MissedItems,
            },
        };
        yield return new object[]
        {
            SharePublicationType.YearRecap,
            new[]
            {
                ShareContentField.PublicDisplayName,
                ShareContentField.Avatar,
                ShareContentField.RideCount,
                ShareContentField.TemporalRatings,
                ShareContentField.GlobalRatings,
                ShareContentField.PublicCaption,
                ShareContentField.GeographicStatistics,
                ShareContentField.MissedItems,
            },
        };
        yield return new object[]
        {
            SharePublicationType.PassportProfile,
            new[]
            {
                ShareContentField.PublicDisplayName,
                ShareContentField.Avatar,
                ShareContentField.RideCount,
                ShareContentField.TemporalRatings,
                ShareContentField.GlobalRatings,
                ShareContentField.PublicCaption,
                ShareContentField.GeographicStatistics,
                ShareContentField.MissedItems,
            },
        };
        yield return new object[]
        {
            SharePublicationType.PersonalRanking,
            new[]
            {
                ShareContentField.PublicDisplayName,
                ShareContentField.Avatar,
                ShareContentField.GlobalRatings,
            },
        };
        yield return new object[]
        {
            SharePublicationType.ProfileComparison,
            new[]
            {
                ShareContentField.PublicDisplayName,
                ShareContentField.Avatar,
                ShareContentField.RideCount,
                ShareContentField.TemporalRatings,
                ShareContentField.GlobalRatings,
            },
        };
    }
}
