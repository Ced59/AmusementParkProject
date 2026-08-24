using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.SocialPublishing.Services;

public sealed class ReferenceSocialPublicationTargetResolver
{
    private readonly IParkOperatorRepository parkOperatorRepository;
    private readonly IParkFounderRepository parkFounderRepository;
    private readonly IAttractionManufacturerRepository attractionManufacturerRepository;

    public ReferenceSocialPublicationTargetResolver(
        IParkOperatorRepository parkOperatorRepository,
        IParkFounderRepository parkFounderRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository)
    {
        this.parkOperatorRepository = parkOperatorRepository;
        this.parkFounderRepository = parkFounderRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
    }

    internal async Task<ResolvedSocialPublicationTarget?> ResolveAsync(
        Uri normalizedUrl,
        IReadOnlyList<string> segments,
        CancellationToken cancellationToken)
    {
        if (segments.Count != 4
            || string.IsNullOrWhiteSpace(segments[2])
            || string.IsNullOrWhiteSpace(segments[3]))
        {
            return null;
        }

        return segments[1].ToLowerInvariant() switch
        {
            "park-operator" => await this.ResolveOperatorAsync(normalizedUrl, segments[2], cancellationToken),
            "park-founder" => await this.ResolveFounderAsync(normalizedUrl, segments[2], cancellationToken),
            "park-manufacturer" => await this.ResolveManufacturerAsync(normalizedUrl, segments[2], cancellationToken),
            _ => null,
        };
    }

    private async Task<ResolvedSocialPublicationTarget?> ResolveOperatorAsync(
        Uri normalizedUrl,
        string id,
        CancellationToken cancellationToken)
    {
        ParkOperator? entity = await this.parkOperatorRepository.GetByIdAsync(id, cancellationToken);
        return entity is null
            || string.IsNullOrWhiteSpace(entity.Id)
            || string.IsNullOrWhiteSpace(entity.Name)
            || entity.AdminReviewStatus == AdminReviewStatus.NotRelevant
                ? null
                : BuildTarget(
                    normalizedUrl,
                    entity.Name,
                    ImageOwnerType.ParkOperator,
                    entity.Id,
                    ImageCategory.Operator);
    }

    private async Task<ResolvedSocialPublicationTarget?> ResolveFounderAsync(
        Uri normalizedUrl,
        string id,
        CancellationToken cancellationToken)
    {
        ParkFounder? entity = await this.parkFounderRepository.GetByIdAsync(id, cancellationToken);
        return entity is null
            || string.IsNullOrWhiteSpace(entity.Id)
            || string.IsNullOrWhiteSpace(entity.Name)
                ? null
                : BuildTarget(
                    normalizedUrl,
                    entity.Name,
                    ImageOwnerType.ParkFounder,
                    entity.Id,
                    ImageCategory.Founder);
    }

    private async Task<ResolvedSocialPublicationTarget?> ResolveManufacturerAsync(
        Uri normalizedUrl,
        string id,
        CancellationToken cancellationToken)
    {
        AttractionManufacturer? entity = await this.attractionManufacturerRepository.GetByIdAsync(id, cancellationToken);
        return entity is null
            || string.IsNullOrWhiteSpace(entity.Id)
            || string.IsNullOrWhiteSpace(entity.Name)
            || !entity.IsVisible
            || entity.AdminReviewStatus == AdminReviewStatus.NotRelevant
                ? null
                : BuildTarget(
                    normalizedUrl,
                    entity.Name,
                    ImageOwnerType.AttractionManufacturer,
                    entity.Id,
                    ImageCategory.Manufacturer);
    }

    private static ResolvedSocialPublicationTarget BuildTarget(
        Uri normalizedUrl,
        string name,
        ImageOwnerType ownerType,
        string ownerId,
        ImageCategory category)
    {
        return new ResolvedSocialPublicationTarget(
            normalizedUrl,
            SocialPublicationTargetKind.Page,
            name,
            name,
            ownerType,
            ownerId,
            category,
            null);
    }
}
