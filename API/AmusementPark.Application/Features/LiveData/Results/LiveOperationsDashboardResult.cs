namespace AmusementPark.Application.Features.LiveData.Results;

public sealed record LiveOperationsDashboardResult(
    string SourceId,
    string SourceDisplayName,
    bool ConfiguredCollectionEnabled,
    bool ConfiguredPublicReadEnabled,
    string AdapterVersion,
    string TransformationVersion,
    string UsagePolicyVersion,
    string TermsUrl,
    DateTime UsagePolicyReviewedAtUtc,
    string AttributionText,
    string AttributionUrl,
    LiveOperationsPollingResult Polling,
    LiveOperationsSummaryResult Summary,
    IReadOnlyCollection<LiveOperationalScopeResult> Scopes,
    DateTime GeneratedAtUtc);
