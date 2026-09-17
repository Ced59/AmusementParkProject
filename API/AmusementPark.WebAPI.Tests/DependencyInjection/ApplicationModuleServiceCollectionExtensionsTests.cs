using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Comments.Commands;
using AmusementPark.Application.Features.Comments.Handlers;
using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Application.Features.Comments.Queries;
using AmusementPark.Application.Features.Comments.Results;
using AmusementPark.Application.Features.Contact.Commands;
using AmusementPark.Application.Features.Contact.Contracts;
using AmusementPark.Application.Features.Contact.Queries;
using AmusementPark.Application.Features.FactualEvents.Commands;
using AmusementPark.Application.Features.FactualEvents.Handlers;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.FactualEvents.Queries;
using AmusementPark.Application.Features.FactualEvents.Results;
using AmusementPark.Application.Features.ParkFit.Handlers;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.ParkPricing.Commands;
using AmusementPark.Application.Features.ParkPricing.Queries;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.TechnicalPages.Commands;
using AmusementPark.Application.Features.TechnicalPages.Queries;
using AmusementPark.Application.Features.TechnicalPages.Results;
using AmusementPark.Application.Features.TechnicalStats.Commands;
using AmusementPark.Application.Features.TechnicalStats.Contracts;
using AmusementPark.Application.Features.TechnicalStats.Queries;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Handlers;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Videos.Commands;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Videos.Queries;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Queries;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;
using AmusementPark.WebAPI.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.DependencyInjection;

public sealed class ApplicationModuleServiceCollectionExtensionsTests
{
    [Fact]
    public void AddApplicationModules_WhenCalled_ShouldRegisterApplicationHandlers()
    {
        ServiceCollection services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        services.AddApplicationModules(configuration);

        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<CreateVideoCommand, ApplicationResult<Video>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<UpdateVideoCommand, ApplicationResult<Video>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<DeleteVideoCommand, ApplicationResult>));
        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<CreateVideoTagCommand, ApplicationResult<VideoTag>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<UpdateVideoTagCommand, ApplicationResult<VideoTag>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<GetVideoByIdQuery, ApplicationResult<Video>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<GetVideosPageQuery, ApplicationResult<PagedResult<Video>>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<ListVideoTagsQuery, ApplicationResult<IReadOnlyCollection<VideoTag>>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<ResolveVideoMetadataQuery, ApplicationResult<ResolvedVideoMetadata>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<SubmitContactGrievanceCommand, ApplicationResult<ContactGrievanceSubmissionResult>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<GetContactGrievancesQuery, ApplicationResult<PagedResult<AmusementPark.Core.Domain.Contact.ContactGrievance>>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<GetTechnicalPagesQuery, ApplicationResult<IReadOnlyCollection<TechnicalPageResult>>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<GetTechnicalPageLinkIndexQuery, ApplicationResult<IReadOnlyCollection<TechnicalPageResult>>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<GetTechnicalPageBySlugQuery, ApplicationResult<TechnicalPageResult>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<UpsertTechnicalPagesJsonCommand, ApplicationResult<TechnicalPageJsonUpsertResult>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<GetTechnicalStatsQuery, ApplicationResult<TechnicalStatsSnapshot>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<UpdateTechnicalStatsSettingsCommand, ApplicationResult<TechnicalStatsSettings>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<GetParkPricingQuery, ApplicationResult<AmusementPark.Core.Domain.Parks.ParkPricing>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<GetParkFitDataQualityPageQuery, ApplicationResult<PagedResult<ParkFitDataQualityOperationsResult>>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<SearchParksByFitQuery, ApplicationResult<ParkFitSearchResult>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<SubmitParkFitSourceReportCommand, ApplicationResult>));
        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<ReviewParkFitSourceReportCommand, ApplicationResult>));
        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<ChangeParkFitOperationalStatusCommand, ApplicationResult>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<GetParkFitSourceReportsQuery, ApplicationResult<PagedResult<ParkFitSourceReportResult>>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<UpsertParkPricingCommand, ApplicationResult<AmusementPark.Core.Domain.Parks.ParkPricing>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<CreateVisitCommand, ApplicationResult<CreateVisitResult>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<ListUserVisitsQuery, ApplicationResult<VisitPageResult>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<GetVisitQuery, ApplicationResult<VisitResult>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<GetPassportItemStatisticsQuery, ApplicationResult<PassportItemStatisticsResult>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<GetPassportParkStatisticsQuery, ApplicationResult<PassportParkStatisticsResult>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<GetPassportYearStatisticsQuery, ApplicationResult<PassportYearStatisticsResult>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<CreateProfileComparisonInvitationCommand, ApplicationResult<ProfileComparisonInvitationCreationResult>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<GetProfileComparisonInvitationPreviewQuery, ApplicationResult<ProfileComparisonInvitationPreviewResult>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<AcceptProfileComparisonInvitationCommand, ApplicationResult<ProfileComparisonInvitationAcceptanceResult>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<AddUserCollectionEntryCommand, ApplicationResult<UserCollectionEntryResult>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<DeleteUserCollectionEntryCommand, ApplicationResult>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<ListMyUserCollectionEntriesQuery, ApplicationResult<IReadOnlyCollection<UserCollectionEntryResult>>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<VerifyFactualChangeEventCommand, ApplicationResult>));
        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<PublishFactualChangeEventCommand, ApplicationResult>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<GetFactualChangeEventsQuery, ApplicationResult<PagedResult<FactualChangeEventAdminResult>>>));
    }

    [Fact]
    public void AddApplicationModules_WhenCalled_ShouldResolveFactualEventAdministrationHandlers()
    {
        ServiceCollection services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        services.AddApplicationModules(configuration);
        services.AddLogging();
        services.AddSingleton(Mock.Of<IFactualChangeEventRepository>());
        services.AddSingleton(Mock.Of<IFactualNotificationDistributionScheduler>());
        services.AddSingleton(Mock.Of<IFactualChangeEventDistributionStateReader>());
        services.AddSingleton(Mock.Of<IParkNameReadRepository>());
        services.AddSingleton(Mock.Of<IParkItemNameReadRepository>());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        ICommandHandler<VerifyFactualChangeEventCommand, ApplicationResult> verifyHandler =
            serviceProvider.GetRequiredService<ICommandHandler<
                VerifyFactualChangeEventCommand,
                ApplicationResult>>();
        ICommandHandler<PublishFactualChangeEventCommand, ApplicationResult> publishHandler =
            serviceProvider.GetRequiredService<ICommandHandler<
                PublishFactualChangeEventCommand,
                ApplicationResult>>();
        IQueryHandler<GetFactualChangeEventsQuery,
            ApplicationResult<PagedResult<FactualChangeEventAdminResult>>> queryHandler =
            serviceProvider.GetRequiredService<IQueryHandler<
                GetFactualChangeEventsQuery,
                ApplicationResult<PagedResult<FactualChangeEventAdminResult>>>>();

        Assert.IsType<VerifyFactualChangeEventCommandHandler>(verifyHandler);
        Assert.IsType<PublishFactualChangeEventCommandHandler>(publishHandler);
        Assert.IsType<GetFactualChangeEventsQueryHandler>(queryHandler);
    }

    [Fact]
    public void AddApplicationModules_WhenCalled_ShouldResolveCommentHandlers()
    {
        ServiceCollection services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        services.AddApplicationModules(configuration);
        services.AddSingleton(Mock.Of<ICommentRepository>());
        services.AddSingleton(Mock.Of<ICommentContentSanitizer>());
        services.AddSingleton(Mock.Of<IUserRepository>());
        services.AddSingleton(Mock.Of<IParkRepository>());
        services.AddSingleton(Mock.Of<IParkItemRepository>());
        services.AddSingleton(Mock.Of<IImageRepository>());
        services.AddSingleton(Mock.Of<IImageBinaryStorage>());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        ICommandHandler<CreateCommentCommand, ApplicationResult<CommentResult>> createHandler =
            serviceProvider.GetRequiredService<ICommandHandler<CreateCommentCommand, ApplicationResult<CommentResult>>>();
        IQueryHandler<GetCommentSummaryQuery, ApplicationResult<CommentSummaryResult>> summaryHandler =
            serviceProvider.GetRequiredService<IQueryHandler<GetCommentSummaryQuery, ApplicationResult<CommentSummaryResult>>>();
        IQueryHandler<GetCommentThreadQuery, ApplicationResult<CommentThreadResult>> threadHandler =
            serviceProvider.GetRequiredService<IQueryHandler<GetCommentThreadQuery, ApplicationResult<CommentThreadResult>>>();

        Assert.IsType<CreateCommentCommandHandler>(createHandler);
        Assert.IsType<GetCommentSummaryQueryHandler>(summaryHandler);
        Assert.IsType<GetCommentThreadQueryHandler>(threadHandler);
    }

    [Fact]
    public void AddApplicationModules_WhenCalled_ShouldResolveAnonymousParkFitSearch()
    {
        ServiceCollection services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        services.AddApplicationModules(configuration);
        services.AddSingleton(Mock.Of<IParkRepository>());
        services.AddSingleton(Mock.Of<IParkItemRepository>());
        services.AddSingleton(Mock.Of<IParkOpeningHoursRepository>());
        services.AddSingleton(Mock.Of<IParkFitCandidatePortfolioReadRepository>());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        IQueryHandler<SearchParksByFitQuery, ApplicationResult<ParkFitSearchResult>> handler =
            serviceProvider.GetRequiredService<IQueryHandler<
                SearchParksByFitQuery,
                ApplicationResult<ParkFitSearchResult>>>();

        Assert.IsType<SearchParksByFitQueryHandler>(handler);
    }

    [Fact]
    public void AddApplicationModules_WhenCalled_ShouldResolveTripPlanHandlers()
    {
        ServiceCollection services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        services.AddLogging();
        services.AddApplicationModules(configuration);
        services.AddSingleton(Mock.Of<ITripPlanRepository>());
        services.AddSingleton(Mock.Of<ITripParkCandidateRepository>());
        services.AddSingleton(Mock.Of<ITripDayPlanRepository>());
        services.AddSingleton(Mock.Of<ITripChildMutationLeaseRepository>());
        services.AddSingleton(Mock.Of<ITripTimeZoneValidator>());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        Assert.IsType<CreateTripPlanCommandHandler>(serviceProvider.GetRequiredService<
            ICommandHandler<CreateTripPlanCommand, ApplicationResult<CreateTripPlanResult>>>());
        Assert.IsType<RenameTripPlanCommandHandler>(serviceProvider.GetRequiredService<
            ICommandHandler<RenameTripPlanCommand, ApplicationResult<TripPlanResult>>>());
        Assert.IsType<SetTripPlanDatesCommandHandler>(serviceProvider.GetRequiredService<
            ICommandHandler<SetTripPlanDatesCommand, ApplicationResult<TripPlanResult>>>());
        Assert.IsType<DeleteTripPlanCommandHandler>(serviceProvider.GetRequiredService<
            ICommandHandler<DeleteTripPlanCommand, ApplicationResult>>());
        Assert.IsType<ListMyTripPlansQueryHandler>(serviceProvider.GetRequiredService<
            IQueryHandler<ListMyTripPlansQuery, ApplicationResult<IReadOnlyCollection<TripPlanResult>>>>());
        Assert.IsType<GetMyTripPlanQueryHandler>(serviceProvider.GetRequiredService<
            IQueryHandler<GetMyTripPlanQuery, ApplicationResult<TripPlanResult>>>());
    }
}
