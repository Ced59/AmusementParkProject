using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.DataSources.Commands;
using AmusementPark.Application.Features.DataSources.Contracts;
using AmusementPark.Application.Features.DataSources.Queries;
using AmusementPark.Application.Features.DataSources.Results;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.DataSources;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Contrôleur générique d'administration des sources externes.
/// </summary>
[ApiController]
[Route("admin/data-sources")]
[Authorize(Roles = "ADMIN")]
[RequireActivatedUnblockedUser]
public sealed class DataSourcesController : ControllerBase
{
    private readonly IQueryHandler<ListDataSourcesQuery, ApplicationResult<IReadOnlyCollection<DataSourceStatusResult>>> listDataSourcesQueryHandler;
    private readonly IQueryHandler<GetDataSourceStatusQuery, ApplicationResult<DataSourceStatusResult>> getDataSourceStatusQueryHandler;
    private readonly IQueryHandler<GetDataSourceSettingsQuery, ApplicationResult<DataSourceSettingsResult>> getDataSourceSettingsQueryHandler;
    private readonly ICommandHandler<UpdateDataSourceSettingsCommand, ApplicationResult<DataSourceSettingsResult>> updateDataSourceSettingsCommandHandler;
    private readonly IQueryHandler<GetLatestDataSourceSessionQuery, ApplicationResult<DataSourceSessionResult?>> getLatestDataSourceSessionQueryHandler;
    private readonly IQueryHandler<GetDataSourceSessionQuery, ApplicationResult<DataSourceSessionResult>> getDataSourceSessionQueryHandler;
    private readonly IQueryHandler<GetDataSourceComparisonResultsQuery, ApplicationResult<DataSourceComparisonPageResult>> getDataSourceComparisonResultsQueryHandler;
    private readonly ICommandHandler<StartDataSourceImportCommand, ApplicationResult<DataSourceSessionResult>> startDataSourceImportCommandHandler;
    private readonly ICommandHandler<ApplyDataSourceComparisonCommand, ApplicationResult<DataSourceApplyResult>> applyDataSourceComparisonCommandHandler;

    public DataSourcesController(
        IQueryHandler<ListDataSourcesQuery, ApplicationResult<IReadOnlyCollection<DataSourceStatusResult>>> listDataSourcesQueryHandler,
        IQueryHandler<GetDataSourceStatusQuery, ApplicationResult<DataSourceStatusResult>> getDataSourceStatusQueryHandler,
        IQueryHandler<GetDataSourceSettingsQuery, ApplicationResult<DataSourceSettingsResult>> getDataSourceSettingsQueryHandler,
        ICommandHandler<UpdateDataSourceSettingsCommand, ApplicationResult<DataSourceSettingsResult>> updateDataSourceSettingsCommandHandler,
        IQueryHandler<GetLatestDataSourceSessionQuery, ApplicationResult<DataSourceSessionResult?>> getLatestDataSourceSessionQueryHandler,
        IQueryHandler<GetDataSourceSessionQuery, ApplicationResult<DataSourceSessionResult>> getDataSourceSessionQueryHandler,
        IQueryHandler<GetDataSourceComparisonResultsQuery, ApplicationResult<DataSourceComparisonPageResult>> getDataSourceComparisonResultsQueryHandler,
        ICommandHandler<StartDataSourceImportCommand, ApplicationResult<DataSourceSessionResult>> startDataSourceImportCommandHandler,
        ICommandHandler<ApplyDataSourceComparisonCommand, ApplicationResult<DataSourceApplyResult>> applyDataSourceComparisonCommandHandler)
    {
        this.listDataSourcesQueryHandler = listDataSourcesQueryHandler;
        this.getDataSourceStatusQueryHandler = getDataSourceStatusQueryHandler;
        this.getDataSourceSettingsQueryHandler = getDataSourceSettingsQueryHandler;
        this.updateDataSourceSettingsCommandHandler = updateDataSourceSettingsCommandHandler;
        this.getLatestDataSourceSessionQueryHandler = getLatestDataSourceSessionQueryHandler;
        this.getDataSourceSessionQueryHandler = getDataSourceSessionQueryHandler;
        this.getDataSourceComparisonResultsQueryHandler = getDataSourceComparisonResultsQueryHandler;
        this.startDataSourceImportCommandHandler = startDataSourceImportCommandHandler;
        this.applyDataSourceComparisonCommandHandler = applyDataSourceComparisonCommandHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseDto<DataSourceStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync([FromQuery] PaginationRequestDto pagination, CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<DataSourceStatusResult>> result = await this.listDataSourcesQueryHandler.HandleAsync(new ListDataSourcesQuery(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PagedResponseDto<DataSourceStatusDto> response = pagination.ToPagedResponse(result.Value, static item => item.ToHttp());
        return this.Ok(response);
    }

    [HttpGet("{sourceKey}/status")]
    [ProducesResponseType(typeof(DataSourceStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatusAsync([FromRoute] string sourceKey, CancellationToken cancellationToken = default)
    {
        ApplicationResult<DataSourceStatusResult> result = await this.getDataSourceStatusQueryHandler.HandleAsync(new GetDataSourceStatusQuery(sourceKey), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("{sourceKey}/settings")]
    [ProducesResponseType(typeof(DataSourceSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettingsAsync([FromRoute] string sourceKey, CancellationToken cancellationToken = default)
    {
        ApplicationResult<DataSourceSettingsResult> result = await this.getDataSourceSettingsQueryHandler.HandleAsync(new GetDataSourceSettingsQuery(sourceKey), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("{sourceKey}/settings")]
    [AdminAudit("data-source.settings.update", "DataSource", TargetIdRouteKey = "sourceKey")]
    [ProducesResponseType(typeof(DataSourceSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSettingsAsync([FromRoute] string sourceKey, [FromBody] UpdateDataSourceSettingsDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<DataSourceSettingsResult> result = await this.updateDataSourceSettingsCommandHandler.HandleAsync(
            new UpdateDataSourceSettingsCommand(sourceKey, dto.ToApplication(sourceKey)),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("{sourceKey}/sessions/latest")]
    [ProducesResponseType(typeof(DataSourceSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetLatestSessionAsync([FromRoute] string sourceKey, CancellationToken cancellationToken = default)
    {
        ApplicationResult<DataSourceSessionResult?> result = await this.getLatestDataSourceSessionQueryHandler.HandleAsync(new GetLatestDataSourceSessionQuery(sourceKey), cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        if (result.Value is null)
        {
            return this.NoContent();
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("{sourceKey}/sessions/{sessionId}")]
    [ProducesResponseType(typeof(DataSourceSessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessionByIdAsync([FromRoute] string sourceKey, [FromRoute] string sessionId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<DataSourceSessionResult> result = await this.getDataSourceSessionQueryHandler.HandleAsync(new GetDataSourceSessionQuery(sourceKey, sessionId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("{sourceKey}/comparison-results")]
    [ProducesResponseType(typeof(DataSourceComparisonPageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComparisonResultsAsync(
        [FromRoute] string sourceKey,
        [FromQuery] string? sessionId,
        [FromQuery] string? entityType,
        [FromQuery] string? changeType,
        [FromQuery] bool? isApplied,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<DataSourceComparisonPageResult> result = await this.getDataSourceComparisonResultsQueryHandler.HandleAsync(
            new GetDataSourceComparisonResultsQuery(sourceKey, sessionId, entityType, changeType, isApplied, page, pageSize),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost("{sourceKey}/import")]
    [AdminAudit("data-source.import.start", "DataSource", TargetIdRouteKey = "sourceKey")]
    [ProducesResponseType(typeof(DataSourceSessionDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> StartImportAsync(
        [FromRoute] string sourceKey,
        [FromBody] StartDataSourceImportRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        string workingDirectoryPath = Path.Combine(Path.GetTempPath(), "amusement-park", "data-sources", sourceKey, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDirectoryPath);

        try
        {
            DataSourceImportDescriptor descriptor = dto.ToApplication(workingDirectoryPath);
            ApplicationResult<DataSourceSessionResult> result = await this.startDataSourceImportCommandHandler.HandleAsync(
                new StartDataSourceImportCommand(sourceKey, descriptor),
                cancellationToken);

            if (!result.IsSuccess || result.Value is null)
            {
                DeleteWorkingDirectorySafe(workingDirectoryPath);
                return this.ToActionResult(result);
            }

            return this.Accepted(result.Value.ToHttp());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            DeleteWorkingDirectorySafe(workingDirectoryPath);
            throw;
        }
        catch (Exception)
        {
            DeleteWorkingDirectorySafe(workingDirectoryPath);
            throw;
        }
    }

    [HttpPost("{sourceKey}/apply")]
    [AdminAudit("data-source.comparison.apply", "DataSource", TargetIdRouteKey = "sourceKey")]
    [ProducesResponseType(typeof(DataSourceApplyResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyAsync([FromRoute] string sourceKey, [FromBody] ApplyDataSourceComparisonRequestDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<DataSourceApplyResult> result = await this.applyDataSourceComparisonCommandHandler.HandleAsync(
            new ApplyDataSourceComparisonCommand(sourceKey, dto.ToApplication()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(new DataSourceApplyResultDto
        {
            AppliedCount = result.Value.AppliedCount,
        });
    }

    private static void DeleteWorkingDirectorySafe(string workingDirectoryPath)
    {
        try
        {
            if (Directory.Exists(workingDirectoryPath))
            {
                Directory.Delete(workingDirectoryPath, true);
            }
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
        {
        }
    }
}
