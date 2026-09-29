namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed class LiveOperationsDashboardDto
{
    public string SourceId { get; set; } = string.Empty;

    public string SourceDisplayName { get; set; } = string.Empty;

    public bool ConfiguredCollectionEnabled { get; set; }

    public bool ConfiguredPublicReadEnabled { get; set; }

    public string AdapterVersion { get; set; } = string.Empty;

    public string TransformationVersion { get; set; } = string.Empty;

    public string UsagePolicyVersion { get; set; } = string.Empty;

    public string TermsUrl { get; set; } = string.Empty;

    public DateTime UsagePolicyReviewedAtUtc { get; set; }

    public string AttributionText { get; set; } = string.Empty;

    public string AttributionUrl { get; set; } = string.Empty;

    public LiveOperationsPollingDto Polling { get; set; } = new LiveOperationsPollingDto();

    public LiveOperationsSummaryDto Summary { get; set; } = new LiveOperationsSummaryDto();

    public IReadOnlyCollection<LiveOperationalScopeDto> Scopes { get; set; } =
        Array.Empty<LiveOperationalScopeDto>();

    public DateTime GeneratedAtUtc { get; set; }
}
