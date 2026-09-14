using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFit.Services;

public sealed class ParkFitEvidenceSourceResolver
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkOpeningHoursRepository openingHoursRepository;

    public ParkFitEvidenceSourceResolver(
        IParkItemRepository parkItemRepository,
        IParkOpeningHoursRepository openingHoursRepository)
    {
        this.parkItemRepository = parkItemRepository;
        this.openingHoursRepository = openingHoursRepository;
    }

    public async Task<ParkFitEvidenceSourceResolution?> ResolveAsync(
        string parkId,
        ParkFitEvidenceKind evidenceKind,
        string? sourceUrl,
        string? sourceReference,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeHttpsUrl(sourceUrl, out string? normalizedSourceUrl)
            || !TryNormalizeReference(sourceReference, out string? normalizedSourceReference))
        {
            return null;
        }

        if (normalizedSourceUrl is null && normalizedSourceReference is null)
        {
            return new ParkFitEvidenceSourceResolution(null, null);
        }

        return evidenceKind switch
        {
            ParkFitEvidenceKind.OpeningCalendar => await this.ResolveOpeningCalendarAsync(
                parkId,
                normalizedSourceUrl,
                normalizedSourceReference,
                cancellationToken),
            ParkFitEvidenceKind.AccessCondition => await this.ResolveAccessConditionAsync(
                parkId,
                normalizedSourceUrl,
                normalizedSourceReference,
                cancellationToken),
            _ => null,
        };
    }

    private async Task<ParkFitEvidenceSourceResolution?> ResolveOpeningCalendarAsync(
        string parkId,
        string? sourceUrl,
        string? sourceReference,
        CancellationToken cancellationToken)
    {
        if (sourceUrl is null || sourceReference is not null)
        {
            return null;
        }

        ParkOpeningHoursSchedule? schedule = await this.openingHoursRepository.GetByParkIdAsync(
            parkId,
            cancellationToken);
        string? trustedSourceUrl = NormalizeTrustedHttpsUrl(schedule?.SourceUrl);
        return string.Equals(sourceUrl, trustedSourceUrl, StringComparison.Ordinal)
            ? new ParkFitEvidenceSourceResolution(trustedSourceUrl, null)
            : null;
    }

    private async Task<ParkFitEvidenceSourceResolution?> ResolveAccessConditionAsync(
        string parkId,
        string? sourceUrl,
        string? sourceReference,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkItem> attractions =
            await this.parkItemRepository.GetVisibleOpenAttractionsByParkIdsAsync(
                new[] { parkId },
                cancellationToken);
        foreach (AttractionAccessCondition condition in attractions
            .Where(static attraction => attraction.AttractionDetails is not null)
            .SelectMany(static attraction => attraction.AttractionDetails!.AccessConditions))
        {
            string? trustedSourceUrl = NormalizeTrustedHttpsUrl(condition.SourceUrl);
            string? trustedSourceReference = NormalizeTrustedReference(condition.SourceReference);
            if (string.Equals(sourceUrl, trustedSourceUrl, StringComparison.Ordinal)
                && string.Equals(sourceReference, trustedSourceReference, StringComparison.Ordinal))
            {
                return new ParkFitEvidenceSourceResolution(
                    trustedSourceUrl,
                    trustedSourceReference);
            }
        }

        return null;
    }

    private static bool TryNormalizeHttpsUrl(string? value, out string? normalized)
    {
        normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized is null)
        {
            return true;
        }

        return normalized.Length <= ParkFitSourceReport.MaximumSourceValueLength
            && Uri.TryCreate(normalized, UriKind.Absolute, out Uri? uri)
            && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(uri.Host)
            && string.IsNullOrEmpty(uri.UserInfo);
    }

    private static bool TryNormalizeReference(string? value, out string? normalized)
    {
        normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return normalized is null
            || normalized.Length <= ParkFitSourceReport.MaximumSourceValueLength;
    }

    private static string? NormalizeTrustedHttpsUrl(string? value)
    {
        return TryNormalizeHttpsUrl(value, out string? normalized) ? normalized : null;
    }

    private static string? NormalizeTrustedReference(string? value)
    {
        return TryNormalizeReference(value, out string? normalized) ? normalized : null;
    }
}
