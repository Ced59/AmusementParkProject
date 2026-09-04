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
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.ParkPricing.Commands;
using AmusementPark.Application.Features.ParkPricing.Queries;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.TechnicalPages.Commands;
using AmusementPark.Application.Features.TechnicalPages.Queries;
using AmusementPark.Application.Features.TechnicalPages.Results;
using AmusementPark.Application.Features.TechnicalStats.Commands;
using AmusementPark.Application.Features.TechnicalStats.Contracts;
using AmusementPark.Application.Features.TechnicalStats.Queries;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Videos.Commands;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Videos.Queries;
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
        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<UpsertParkPricingCommand, ApplicationResult<AmusementPark.Core.Domain.Parks.ParkPricing>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(ICommandHandler<CreateVisitCommand, ApplicationResult<CreateVisitResult>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<ListUserVisitsQuery, ApplicationResult<VisitPageResult>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<GetVisitQuery, ApplicationResult<VisitResult>>));
        Assert.Contains(services, static service => service.ServiceType == typeof(IQueryHandler<GetPassportItemStatisticsQuery, ApplicationResult<PassportItemStatisticsResult>>));
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
}
