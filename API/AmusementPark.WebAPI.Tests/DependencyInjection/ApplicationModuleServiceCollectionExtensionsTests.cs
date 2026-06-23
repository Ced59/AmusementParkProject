using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Contact.Commands;
using AmusementPark.Application.Features.Contact.Contracts;
using AmusementPark.Application.Features.Contact.Queries;
using AmusementPark.Application.Features.TechnicalPages.Commands;
using AmusementPark.Application.Features.TechnicalPages.Queries;
using AmusementPark.Application.Features.TechnicalPages.Results;
using AmusementPark.Application.Features.TechnicalStats.Contracts;
using AmusementPark.Application.Features.TechnicalStats.Queries;
using AmusementPark.Application.Features.Videos.Commands;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Application.Features.Videos.Queries;
using AmusementPark.Core.Domain.Videos;
using AmusementPark.WebAPI.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    }
}
