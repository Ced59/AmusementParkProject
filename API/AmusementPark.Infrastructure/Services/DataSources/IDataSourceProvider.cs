using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.DataSources.Contracts;
using AmusementPark.Application.Features.DataSources.Ports;
using AmusementPark.Application.Features.DataSources.Results;

namespace AmusementPark.Infrastructure.Services.DataSources;

internal interface IDataSourceProvider
{
    string SourceKey { get; }

    Task<DataSourceStatusResult> GetStatusAsync(CancellationToken cancellationToken);

    Task<DataSourceSettingsResult> GetSettingsAsync(CancellationToken cancellationToken);

    Task<DataSourceSettingsResult> UpdateSettingsAsync(DataSourceSettingsResult settings, CancellationToken cancellationToken);

    Task<DataSourceSessionResult?> GetLatestSessionAsync(CancellationToken cancellationToken);

    Task<DataSourceSessionResult?> GetSessionByIdAsync(string sessionId, CancellationToken cancellationToken);

    Task<DataSourceComparisonPageResult> GetComparisonResultsAsync(
        string? sessionId,
        string? entityType,
        string? changeType,
        bool? isApplied,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<DataSourceSessionResult> StartImportAsync(DataSourceImportDescriptor importDescriptor, CancellationToken cancellationToken);

    Task<DataSourceApplyResult> ApplyComparisonAsync(DataSourceApplyRequest request, CancellationToken cancellationToken);
}
