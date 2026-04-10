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

internal sealed class DataSourceAdministrationService : IDataSourceAdministrationService
{
    private readonly IReadOnlyDictionary<string, IDataSourceProvider> providersByKey;

    public DataSourceAdministrationService(IEnumerable<IDataSourceProvider> providers)
    {
        this.providersByKey = providers.ToDictionary(provider => provider.SourceKey, provider => provider, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<ApplicationResult<IReadOnlyCollection<DataSourceStatusResult>>> ListSourcesAsync(CancellationToken cancellationToken)
    {
        List<DataSourceStatusResult> results = new List<DataSourceStatusResult>();
        foreach (IDataSourceProvider provider in this.providersByKey.Values.OrderBy(provider => provider.SourceKey, StringComparer.OrdinalIgnoreCase))
        {
            DataSourceStatusResult result = await provider.GetStatusAsync(cancellationToken);
            results.Add(result);
        }

        return ApplicationResult<IReadOnlyCollection<DataSourceStatusResult>>.Success(results);
    }

    public async Task<ApplicationResult<DataSourceStatusResult>> GetStatusAsync(string sourceKey, CancellationToken cancellationToken)
    {
        IDataSourceProvider? provider = this.ResolveProvider(sourceKey);
        if (provider is null)
        {
            return ApplicationResult<DataSourceStatusResult>.Failure(AmusementPark.Application.Features.DataSources.DataSourcesApplicationErrors.UnsupportedSource(sourceKey));
        }

        DataSourceStatusResult result = await provider.GetStatusAsync(cancellationToken);
        return ApplicationResult<DataSourceStatusResult>.Success(result);
    }

    public async Task<ApplicationResult<DataSourceSettingsResult>> GetSettingsAsync(string sourceKey, CancellationToken cancellationToken)
    {
        IDataSourceProvider? provider = this.ResolveProvider(sourceKey);
        if (provider is null)
        {
            return ApplicationResult<DataSourceSettingsResult>.Failure(AmusementPark.Application.Features.DataSources.DataSourcesApplicationErrors.UnsupportedSource(sourceKey));
        }

        DataSourceSettingsResult result = await provider.GetSettingsAsync(cancellationToken);
        return ApplicationResult<DataSourceSettingsResult>.Success(result);
    }

    public async Task<ApplicationResult<DataSourceSettingsResult>> UpdateSettingsAsync(string sourceKey, DataSourceSettingsResult settings, CancellationToken cancellationToken)
    {
        IDataSourceProvider? provider = this.ResolveProvider(sourceKey);
        if (provider is null)
        {
            return ApplicationResult<DataSourceSettingsResult>.Failure(AmusementPark.Application.Features.DataSources.DataSourcesApplicationErrors.UnsupportedSource(sourceKey));
        }

        DataSourceSettingsResult result = await provider.UpdateSettingsAsync(settings, cancellationToken);
        return ApplicationResult<DataSourceSettingsResult>.Success(result);
    }

    public async Task<ApplicationResult<DataSourceSessionResult?>> GetLatestSessionAsync(string sourceKey, CancellationToken cancellationToken)
    {
        IDataSourceProvider? provider = this.ResolveProvider(sourceKey);
        if (provider is null)
        {
            return ApplicationResult<DataSourceSessionResult?>.Failure(AmusementPark.Application.Features.DataSources.DataSourcesApplicationErrors.UnsupportedSource(sourceKey));
        }

        DataSourceSessionResult? result = await provider.GetLatestSessionAsync(cancellationToken);
        return ApplicationResult<DataSourceSessionResult?>.Success(result);
    }

    public async Task<ApplicationResult<DataSourceSessionResult>> GetSessionByIdAsync(string sourceKey, string sessionId, CancellationToken cancellationToken)
    {
        IDataSourceProvider? provider = this.ResolveProvider(sourceKey);
        if (provider is null)
        {
            return ApplicationResult<DataSourceSessionResult>.Failure(AmusementPark.Application.Features.DataSources.DataSourcesApplicationErrors.UnsupportedSource(sourceKey));
        }

        DataSourceSessionResult? result = await provider.GetSessionByIdAsync(sessionId, cancellationToken);
        if (result is null)
        {
            return ApplicationResult<DataSourceSessionResult>.Failure(ApplicationErrors.EntityNotFound("DataSourceSession", sessionId));
        }

        return ApplicationResult<DataSourceSessionResult>.Success(result);
    }

    public async Task<ApplicationResult<DataSourceComparisonPageResult>> GetComparisonResultsAsync(
        string sourceKey,
        string? sessionId,
        string? entityType,
        string? changeType,
        bool? isApplied,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IDataSourceProvider? provider = this.ResolveProvider(sourceKey);
        if (provider is null)
        {
            return ApplicationResult<DataSourceComparisonPageResult>.Failure(AmusementPark.Application.Features.DataSources.DataSourcesApplicationErrors.UnsupportedSource(sourceKey));
        }

        DataSourceComparisonPageResult result = await provider.GetComparisonResultsAsync(sessionId, entityType, changeType, isApplied, page, pageSize, cancellationToken);
        return ApplicationResult<DataSourceComparisonPageResult>.Success(result);
    }

    public async Task<ApplicationResult<DataSourceSessionResult>> StartImportAsync(string sourceKey, DataSourceImportDescriptor importDescriptor, CancellationToken cancellationToken)
    {
        IDataSourceProvider? provider = this.ResolveProvider(sourceKey);
        if (provider is null)
        {
            return ApplicationResult<DataSourceSessionResult>.Failure(AmusementPark.Application.Features.DataSources.DataSourcesApplicationErrors.UnsupportedSource(sourceKey));
        }

        try
        {
            DataSourceSessionResult result = await provider.StartImportAsync(importDescriptor, cancellationToken);
            return ApplicationResult<DataSourceSessionResult>.Success(result);
        }
        catch (InvalidOperationException exception)
        {
            return ApplicationResult<DataSourceSessionResult>.Failure(ApplicationError.Conflict("data-sources.import.conflict", exception.Message));
        }
        catch (ArgumentException exception)
        {
            return ApplicationResult<DataSourceSessionResult>.Failure(ApplicationError.Validation("data-sources.import.invalid", exception.Message));
        }
    }

    public async Task<ApplicationResult<DataSourceApplyResult>> ApplyComparisonAsync(string sourceKey, DataSourceApplyRequest request, CancellationToken cancellationToken)
    {
        IDataSourceProvider? provider = this.ResolveProvider(sourceKey);
        if (provider is null)
        {
            return ApplicationResult<DataSourceApplyResult>.Failure(AmusementPark.Application.Features.DataSources.DataSourcesApplicationErrors.UnsupportedSource(sourceKey));
        }

        DataSourceApplyResult result = await provider.ApplyComparisonAsync(request, cancellationToken);
        return ApplicationResult<DataSourceApplyResult>.Success(result);
    }

    private IDataSourceProvider? ResolveProvider(string sourceKey)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
        {
            return null;
        }

        this.providersByKey.TryGetValue(sourceKey.Trim(), out IDataSourceProvider? provider);
        return provider;
    }
}
