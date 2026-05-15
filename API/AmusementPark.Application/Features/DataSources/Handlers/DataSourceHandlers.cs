using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.DataSources.Commands;
using AmusementPark.Application.Features.DataSources.Ports;
using AmusementPark.Application.Features.DataSources.Queries;
using AmusementPark.Application.Features.DataSources.Results;

namespace AmusementPark.Application.Features.DataSources.Handlers;

public sealed class ListDataSourcesQueryHandler : IQueryHandler<ListDataSourcesQuery, ApplicationResult<IReadOnlyCollection<DataSourceStatusResult>>>
{
    private readonly IDataSourceAdministrationService service;

    public ListDataSourcesQueryHandler(IDataSourceAdministrationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<IReadOnlyCollection<DataSourceStatusResult>>> HandleAsync(ListDataSourcesQuery query, CancellationToken cancellationToken)
    {
        return this.service.ListSourcesAsync(cancellationToken);
    }
}

public sealed class GetDataSourceStatusQueryHandler : IQueryHandler<GetDataSourceStatusQuery, ApplicationResult<DataSourceStatusResult>>
{
    private readonly IDataSourceAdministrationService service;

    public GetDataSourceStatusQueryHandler(IDataSourceAdministrationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<DataSourceStatusResult>> HandleAsync(GetDataSourceStatusQuery query, CancellationToken cancellationToken)
    {
        return this.service.GetStatusAsync(query.SourceKey, cancellationToken);
    }
}

public sealed class GetDataSourceSettingsQueryHandler : IQueryHandler<GetDataSourceSettingsQuery, ApplicationResult<DataSourceSettingsResult>>
{
    private readonly IDataSourceAdministrationService service;

    public GetDataSourceSettingsQueryHandler(IDataSourceAdministrationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<DataSourceSettingsResult>> HandleAsync(GetDataSourceSettingsQuery query, CancellationToken cancellationToken)
    {
        return this.service.GetSettingsAsync(query.SourceKey, cancellationToken);
    }
}

public sealed class UpdateDataSourceSettingsCommandHandler : ICommandHandler<UpdateDataSourceSettingsCommand, ApplicationResult<DataSourceSettingsResult>>
{
    private readonly IDataSourceAdministrationService service;

    public UpdateDataSourceSettingsCommandHandler(IDataSourceAdministrationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<DataSourceSettingsResult>> HandleAsync(UpdateDataSourceSettingsCommand command, CancellationToken cancellationToken)
    {
        return this.service.UpdateSettingsAsync(command.SourceKey, command.Settings, cancellationToken);
    }
}

public sealed class GetLatestDataSourceSessionQueryHandler : IQueryHandler<GetLatestDataSourceSessionQuery, ApplicationResult<DataSourceSessionResult?>>
{
    private readonly IDataSourceAdministrationService service;

    public GetLatestDataSourceSessionQueryHandler(IDataSourceAdministrationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<DataSourceSessionResult?>> HandleAsync(GetLatestDataSourceSessionQuery query, CancellationToken cancellationToken)
    {
        return this.service.GetLatestSessionAsync(query.SourceKey, cancellationToken);
    }
}

public sealed class GetDataSourceSessionQueryHandler : IQueryHandler<GetDataSourceSessionQuery, ApplicationResult<DataSourceSessionResult>>
{
    private readonly IDataSourceAdministrationService service;

    public GetDataSourceSessionQueryHandler(IDataSourceAdministrationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<DataSourceSessionResult>> HandleAsync(GetDataSourceSessionQuery query, CancellationToken cancellationToken)
    {
        return this.service.GetSessionByIdAsync(query.SourceKey, query.SessionId, cancellationToken);
    }
}

public sealed class GetDataSourceComparisonResultsQueryHandler : IQueryHandler<GetDataSourceComparisonResultsQuery, ApplicationResult<DataSourceComparisonPageResult>>
{
    private readonly IDataSourceAdministrationService service;

    public GetDataSourceComparisonResultsQueryHandler(IDataSourceAdministrationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<DataSourceComparisonPageResult>> HandleAsync(GetDataSourceComparisonResultsQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 0 || query.PageSize <= 0)
        {
            return Task.FromResult(ApplicationResult<DataSourceComparisonPageResult>.Failure(ApplicationErrors.InvalidPagination()));
        }

        return this.service.GetComparisonResultsAsync(
            query.SourceKey,
            query.SessionId,
            query.EntityType,
            query.ChangeType,
            query.IsApplied,
            query.Page,
            query.PageSize,
            cancellationToken);
    }
}

public sealed class StartDataSourceImportCommandHandler : ICommandHandler<StartDataSourceImportCommand, ApplicationResult<DataSourceSessionResult>>
{
    private readonly IDataSourceAdministrationService service;

    public StartDataSourceImportCommandHandler(IDataSourceAdministrationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<DataSourceSessionResult>> HandleAsync(StartDataSourceImportCommand command, CancellationToken cancellationToken)
    {
        return this.service.StartImportAsync(command.SourceKey, command.ImportDescriptor, cancellationToken);
    }
}

public sealed class ApplyDataSourceComparisonCommandHandler : ICommandHandler<ApplyDataSourceComparisonCommand, ApplicationResult<DataSourceApplyResult>>
{
    private readonly IDataSourceAdministrationService service;

    public ApplyDataSourceComparisonCommandHandler(IDataSourceAdministrationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<DataSourceApplyResult>> HandleAsync(ApplyDataSourceComparisonCommand command, CancellationToken cancellationToken)
    {
        return this.service.ApplyComparisonAsync(command.SourceKey, command.Request, cancellationToken);
    }
}
