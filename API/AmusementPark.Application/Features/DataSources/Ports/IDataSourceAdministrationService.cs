using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.DataSources.Contracts;
using AmusementPark.Application.Features.DataSources.Results;

namespace AmusementPark.Application.Features.DataSources.Ports;

/// <summary>
/// Port applicatif générique d'administration des sources externes.
/// </summary>
public interface IDataSourceAdministrationService
{
    Task<ApplicationResult<IReadOnlyCollection<DataSourceStatusResult>>> ListSourcesAsync(CancellationToken cancellationToken);

    Task<ApplicationResult<DataSourceStatusResult>> GetStatusAsync(string sourceKey, CancellationToken cancellationToken);

    Task<ApplicationResult<DataSourceSettingsResult>> GetSettingsAsync(string sourceKey, CancellationToken cancellationToken);

    Task<ApplicationResult<DataSourceSettingsResult>> UpdateSettingsAsync(string sourceKey, DataSourceSettingsResult settings, CancellationToken cancellationToken);

    Task<ApplicationResult<DataSourceSessionResult?>> GetLatestSessionAsync(string sourceKey, CancellationToken cancellationToken);

    Task<ApplicationResult<DataSourceSessionResult>> GetSessionByIdAsync(string sourceKey, string sessionId, CancellationToken cancellationToken);

    Task<ApplicationResult<DataSourceComparisonPageResult>> GetComparisonResultsAsync(
        string sourceKey,
        string? sessionId,
        string? entityType,
        string? changeType,
        bool? isApplied,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ApplicationResult<DataSourceSessionResult>> StartImportAsync(string sourceKey, DataSourceImportDescriptor importDescriptor, CancellationToken cancellationToken);

    Task<ApplicationResult<DataSourceApplyResult>> ApplyComparisonAsync(string sourceKey, DataSourceApplyRequest request, CancellationToken cancellationToken);
}
