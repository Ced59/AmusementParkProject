using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Queries;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Features.StandaloneAttractions.Handlers;

public sealed class GetStandaloneAttractionDataCompletenessScoreQueryHandler :
    IQueryHandler<GetStandaloneAttractionDataCompletenessScoreQuery, ApplicationResult<DataCompletenessScore>>
{
    private readonly IStandaloneAttractionRepository attractionRepository;
    private readonly IImageRepository imageRepository;
    private readonly IHistoryEventRepository historyRepository;
    private readonly IParkOperatorRepository operatorRepository;
    private readonly IAttractionManufacturerRepository manufacturerRepository;
    private readonly IStandaloneAttractionOpeningHoursRepository openingHoursRepository;
    private readonly IStandaloneAttractionPricingRepository pricingRepository;
    private readonly TimeProvider timeProvider;

    public GetStandaloneAttractionDataCompletenessScoreQueryHandler(
        IStandaloneAttractionRepository attractionRepository,
        IImageRepository imageRepository,
        IHistoryEventRepository historyRepository,
        IParkOperatorRepository operatorRepository,
        IAttractionManufacturerRepository manufacturerRepository,
        IStandaloneAttractionOpeningHoursRepository openingHoursRepository,
        IStandaloneAttractionPricingRepository pricingRepository,
        TimeProvider? timeProvider = null)
    {
        this.attractionRepository = attractionRepository;
        this.imageRepository = imageRepository;
        this.historyRepository = historyRepository;
        this.operatorRepository = operatorRepository;
        this.manufacturerRepository = manufacturerRepository;
        this.openingHoursRepository = openingHoursRepository;
        this.pricingRepository = pricingRepository;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<DataCompletenessScore>> HandleAsync(
        GetStandaloneAttractionDataCompletenessScoreQuery query,
        CancellationToken cancellationToken = default)
    {
        string id = (query.StandaloneAttractionId ?? string.Empty).Trim();
        StandaloneAttraction? attraction = id.Length == 0 ? null
            : await this.attractionRepository.GetByIdAsync(id, query.IncludeHidden, cancellationToken);
        if (attraction is null)
        {
            return ApplicationResult<DataCompletenessScore>.Failure(
                StandaloneAttractionVisitorInformationApplicationErrors.AttractionNotFound());
        }

        string? operatorId = attraction.OperatorId;
        string? manufacturerId = attraction.AttractionDetails?.ManufacturerId;
        bool operating = ParkItemStatusNormalizer.IsOperating(attraction.AttractionDetails?.Status);
        Task<IReadOnlyCollection<Image>> imagesTask = this.imageRepository.GetByOwnersAsync(
            ImageOwnerType.StandaloneAttraction, new[] { id }, null, cancellationToken);
        Task<IReadOnlyCollection<HistoryEvent>> historyTask = this.historyRepository.GetOwnerTimelineAsync(
            HistoryEntityType.StandaloneAttraction, id, true, cancellationToken);
        Task<ParkOperator?> operatorTask = string.IsNullOrWhiteSpace(operatorId)
            ? Task.FromResult<ParkOperator?>(null) : this.operatorRepository.GetByIdAsync(operatorId, cancellationToken);
        Task<AttractionManufacturer?> manufacturerTask = string.IsNullOrWhiteSpace(manufacturerId)
            ? Task.FromResult<AttractionManufacturer?>(null) : this.manufacturerRepository.GetByIdAsync(manufacturerId, cancellationToken);
        Task<IReadOnlyCollection<Image>> operatorImagesTask = this.LoadReferenceImagesAsync(ImageOwnerType.ParkOperator, operatorId, cancellationToken);
        Task<IReadOnlyCollection<Image>> manufacturerImagesTask = this.LoadReferenceImagesAsync(ImageOwnerType.AttractionManufacturer, manufacturerId, cancellationToken);
        Task<ParkOpeningHoursSchedule?> hoursTask = operating
            ? this.openingHoursRepository.GetByStandaloneAttractionIdAsync(id, cancellationToken)
            : Task.FromResult<ParkOpeningHoursSchedule?>(null);
        Task<ParkPricingEntity?> pricingTask = operating
            ? this.pricingRepository.GetByStandaloneAttractionIdAsync(id, cancellationToken)
            : Task.FromResult<ParkPricingEntity?>(null);
        await Task.WhenAll(imagesTask, historyTask, operatorTask, manufacturerTask,
            operatorImagesTask, manufacturerImagesTask, hoursTask, pricingTask);

        StandaloneAttractionDataCompletenessContext context = new()
        {
            ProjectForPublication = query.ProjectForPublication,
            AsOfUtc = this.timeProvider.GetUtcNow().UtcDateTime,
            Images = await imagesTask,
            HistoryEvents = await historyTask,
            Operator = await operatorTask,
            Manufacturer = await manufacturerTask,
            ReferenceImages = (await operatorImagesTask).Concat(await manufacturerImagesTask).ToArray(),
            OpeningHours = await hoursTask,
            Pricing = await pricingTask,
        };
        return ApplicationResult<DataCompletenessScore>.Success(attraction.CalculateDataCompletenessScore(context));
    }

    private Task<IReadOnlyCollection<Image>> LoadReferenceImagesAsync(ImageOwnerType ownerType, string? id, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(id) ? Task.FromResult<IReadOnlyCollection<Image>>(Array.Empty<Image>())
            : this.imageRepository.GetByOwnersAsync(ownerType, new[] { id }, null, cancellationToken);
}
