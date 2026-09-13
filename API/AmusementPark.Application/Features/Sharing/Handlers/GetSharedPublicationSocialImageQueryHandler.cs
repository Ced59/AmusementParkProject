using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class GetSharedPublicationSocialImageQueryHandler
    : IQueryHandler<GetSharedPublicationSocialImageQuery, ApplicationResult<ShareSocialImageRenderResult>>
{
    private static readonly IReadOnlySet<string> SupportedLanguages = new HashSet<string>(
        new[] { "de", "en", "es", "fr", "it", "nl", "pl", "pt" },
        StringComparer.Ordinal);

    private readonly IQueryHandler<GetSharedVisitRecapQuery, ApplicationResult<SharedVisitRecapResult>> visitHandler;
    private readonly IQueryHandler<GetSharedYearRecapQuery, ApplicationResult<SharedYearRecapResult>> yearHandler;
    private readonly IQueryHandler<GetSharedPassportProfileQuery, ApplicationResult<SharedPassportProfileResult>> passportHandler;
    private readonly IShareSocialImageRenderer renderer;

    public GetSharedPublicationSocialImageQueryHandler(
        IQueryHandler<GetSharedVisitRecapQuery, ApplicationResult<SharedVisitRecapResult>> visitHandler,
        IQueryHandler<GetSharedYearRecapQuery, ApplicationResult<SharedYearRecapResult>> yearHandler,
        IQueryHandler<GetSharedPassportProfileQuery, ApplicationResult<SharedPassportProfileResult>> passportHandler,
        IShareSocialImageRenderer renderer)
    {
        this.visitHandler = visitHandler ?? throw new ArgumentNullException(nameof(visitHandler));
        this.yearHandler = yearHandler ?? throw new ArgumentNullException(nameof(yearHandler));
        this.passportHandler = passportHandler ?? throw new ArgumentNullException(nameof(passportHandler));
        this.renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public async Task<ApplicationResult<ShareSocialImageRenderResult>> HandleAsync(
        GetSharedPublicationSocialImageQuery query,
        CancellationToken cancellationToken = default)
    {
        string language = query.Language?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!SupportedLanguages.Contains(language))
        {
            return ApplicationResult<ShareSocialImageRenderResult>.Failure(
                SharingApplicationErrors.InvalidSocialImageLanguage());
        }

        ApplicationResult<ShareSocialImageModel> modelResult = query.PublicationType switch
        {
            SharePublicationType.VisitRecap => await this.BuildVisitModelAsync(
                query,
                language,
                cancellationToken),
            SharePublicationType.YearRecap => await this.BuildYearModelAsync(
                query,
                language,
                cancellationToken),
            SharePublicationType.PassportProfile => await this.BuildPassportModelAsync(
                query,
                language,
                cancellationToken),
            _ => ApplicationResult<ShareSocialImageModel>.Failure(
                SharingApplicationErrors.SocialImageNotAvailable()),
        };
        if (!modelResult.IsSuccess || modelResult.Value is null)
        {
            return ApplicationResult<ShareSocialImageRenderResult>.Failure(modelResult.Errors);
        }

        ShareSocialImageRenderResult image = await this.renderer.RenderAsync(
            modelResult.Value,
            cancellationToken);
        return ApplicationResult<ShareSocialImageRenderResult>.Success(image);
    }

    private async Task<ApplicationResult<ShareSocialImageModel>> BuildVisitModelAsync(
        GetSharedPublicationSocialImageQuery query,
        string language,
        CancellationToken cancellationToken)
    {
        ApplicationResult<SharedVisitRecapResult> result = await this.visitHandler.HandleAsync(
            new GetSharedVisitRecapQuery(query.ShareId),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return ApplicationResult<ShareSocialImageModel>.Failure(result.Errors);
        }

        if (result.Value.PublicationVersion != query.PublicationVersion)
        {
            return VersionMismatch();
        }

        VisitRecapSharePreviewResult content = result.Value.Content;
        List<ShareSocialImageMetric> metrics = new List<ShareSocialImageMetric>(3);
        if (content.DistinctItemCount.HasValue)
        {
            metrics.Add(new ShareSocialImageMetric(
                ShareSocialImageMetricKind.Attractions,
                content.DistinctItemCount.Value));
        }

        if (content.TotalRideCount.HasValue)
        {
            metrics.Add(new ShareSocialImageMetric(
                ShareSocialImageMetricKind.Rides,
                content.TotalRideCount.Value));
        }

        if (content.ParkRating.HasValue)
        {
            metrics.Add(new ShareSocialImageMetric(
                ShareSocialImageMetricKind.Rating,
                content.ParkRating.Value));
        }

        ShareSocialImageDate? date = content.Date is null
            ? null
            : new ShareSocialImageDate(
                content.Date.Year,
                content.Date.Month,
                content.Date.Day,
                content.Date.Precision);
        return ApplicationResult<ShareSocialImageModel>.Success(
            new ShareSocialImageModel(
                query.ShareId,
                query.PublicationType,
                language,
                content.ParkName,
                date,
                null,
                metrics,
                content.TopRatedItem?.Name ?? content.MostRepeatedItem?.Name,
                query.PublicationVersion));
    }

    private async Task<ApplicationResult<ShareSocialImageModel>> BuildYearModelAsync(
        GetSharedPublicationSocialImageQuery query,
        string language,
        CancellationToken cancellationToken)
    {
        ApplicationResult<SharedYearRecapResult> result = await this.yearHandler.HandleAsync(
            new GetSharedYearRecapQuery(query.ShareId),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return ApplicationResult<ShareSocialImageModel>.Failure(result.Errors);
        }

        if (result.Value.PublicationVersion != query.PublicationVersion)
        {
            return VersionMismatch();
        }

        YearRecapSharePreviewResult content = result.Value.Content;
        List<ShareSocialImageMetric> metrics = new List<ShareSocialImageMetric>(3);
        if (content.ParkCount.HasValue)
        {
            metrics.Add(new ShareSocialImageMetric(
                ShareSocialImageMetricKind.Parks,
                content.ParkCount.Value));
        }

        metrics.Add(new ShareSocialImageMetric(
            ShareSocialImageMetricKind.Visits,
            content.VisitCount));
        if (content.TotalRideCount.HasValue)
        {
            metrics.Add(new ShareSocialImageMetric(
                ShareSocialImageMetricKind.Rides,
                content.TotalRideCount.Value));
        }

        return ApplicationResult<ShareSocialImageModel>.Success(
            new ShareSocialImageModel(
                query.ShareId,
                query.PublicationType,
                language,
                null,
                null,
                content.Year,
                metrics.Take(3).ToList(),
                content.TopRatedItem?.Name ?? content.MostRepeatedItem?.Name,
                query.PublicationVersion));
    }

    private async Task<ApplicationResult<ShareSocialImageModel>> BuildPassportModelAsync(
        GetSharedPublicationSocialImageQuery query,
        string language,
        CancellationToken cancellationToken)
    {
        ApplicationResult<SharedPassportProfileResult> result = await this.passportHandler.HandleAsync(
            new GetSharedPassportProfileQuery(query.ShareId),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return ApplicationResult<ShareSocialImageModel>.Failure(result.Errors);
        }

        if (result.Value.PublicationVersion != query.PublicationVersion)
        {
            return VersionMismatch();
        }

        PassportProfileSharePreviewResult content = result.Value.Content;
        List<ShareSocialImageMetric> metrics = new List<ShareSocialImageMetric>(3);
        if (content.ParkCount.HasValue)
        {
            metrics.Add(new ShareSocialImageMetric(
                ShareSocialImageMetricKind.Parks,
                content.ParkCount.Value));
        }

        if (content.VisitCount.HasValue)
        {
            metrics.Add(new ShareSocialImageMetric(
                ShareSocialImageMetricKind.Visits,
                content.VisitCount.Value));
        }

        if (content.TotalRideCount.HasValue)
        {
            metrics.Add(new ShareSocialImageMetric(
                ShareSocialImageMetricKind.Rides,
                content.TotalRideCount.Value));
        }

        return ApplicationResult<ShareSocialImageModel>.Success(
            new ShareSocialImageModel(
                query.ShareId,
                query.PublicationType,
                language,
                content.DisplayName,
                null,
                null,
                metrics,
                content.Parks.FirstOrDefault()?.Name,
                query.PublicationVersion));
    }

    private static ApplicationResult<ShareSocialImageModel> VersionMismatch()
    {
        return ApplicationResult<ShareSocialImageModel>.Failure(
            SharingApplicationErrors.SocialImageNotAvailable());
    }
}
