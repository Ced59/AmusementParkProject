using Dtos.Parks.Logos;
using Entities.Model.Parks;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces.Images.Logos;
using static Entities.Model.Errors.ErrorCodes;

namespace Services.Implementations.Images.Logos;

public class ParkLogosService : IParkLogosService
{
    private readonly IParkLogosQueryHandler parkLogosQueryHandler;
    private readonly IParksQueryHandler parksQueryHandler;

    public ParkLogosService(
        IParkLogosQueryHandler parkLogosQueryHandler,
        IParksQueryHandler parksQueryHandler)
    {
        this.parkLogosQueryHandler = parkLogosQueryHandler;
        this.parksQueryHandler = parksQueryHandler;
    }

    public async Task<OneOf<ParkLogoDto, ErrorDetail>> AddLogoAsync(
        string parkId,
        ParkLogoCreateDto request)
    {
        // 1) Vérifier que le parc existe
        Park? park = await parksQueryHandler.GetParkByIdAsync(parkId);
        if (park == null)
        {
            return ParkNotExists;
        }

        // 2) Créer le ParkLogo avec IsCurrent = true
        ParkLogo logo = new()
        {
            ParkId = parkId,
            ImageId = request.ImageId,
            Description = request.Description,
            IsCurrent = true
        };

        ParkLogo inserted = await parkLogosQueryHandler.InsertAsync(logo);

        // 3) Désactiver les anciens logos
        await parkLogosQueryHandler.UnsetCurrentForParkAsync(parkId, inserted.Id);

        // 4) Mettre à jour le parc (CurrentLogoImageId)
        await parksQueryHandler.UpdateCurrentLogoAsync(parkId, request.ImageId);

        return ToDto(inserted);
    }

    public async Task<OneOf<ParkLogoDto, ErrorDetail>> GetCurrentLogoAsync(string parkId)
    {
        ParkLogo? logo = await parkLogosQueryHandler.GetCurrentByParkIdAsync(parkId);
        if (logo == null)
        {
            return ParkLogoNotExists;
        }

        return ToDto(logo);
    }

    public async Task<OneOf<IEnumerable<ParkLogoDto>, ErrorDetail>> GetLogosHistoryAsync(string parkId)
    {
        IReadOnlyList<ParkLogo> logos = await parkLogosQueryHandler.GetByParkIdAsync(parkId);

        if (logos.Count == 0)
        {
            return ParkLogoNotExists;
        }

        IEnumerable<ParkLogoDto> dtos = logos.Select(ToDto);
        return OneOf<IEnumerable<ParkLogoDto>, ErrorDetail>.FromT0(dtos);
    }

    public async Task<OneOf<ParkLogoDto, ErrorDetail>> SetCurrentLogoAsync(string logoId)
    {
        ParkLogo? logo = await parkLogosQueryHandler.GetByIdAsync(logoId);
        if (logo == null)
        {
            return ParkLogoNotExists;
        }

        // 1) set IsCurrent = true pour ce logo
        logo.IsCurrent = true;
        await parkLogosQueryHandler.UpdateAsync(logo);

        // 2) unset pour les autres du parc
        await parkLogosQueryHandler.UnsetCurrentForParkAsync(logo.ParkId, logo.Id);

        // 3) mettre à jour le parc
        await parksQueryHandler.UpdateCurrentLogoAsync(logo.ParkId, logo.ImageId);

        return ToDto(logo);
    }

    public async Task<OneOf<bool, ErrorDetail>> DeleteLogoAsync(string logoId)
    {
        ParkLogo? logo = await parkLogosQueryHandler.GetByIdAsync(logoId);
        if (logo == null)
        {
            return ParkLogoNotExists;
        }

        bool wasCurrent = logo.IsCurrent;
        string parkId = logo.ParkId;

        await parkLogosQueryHandler.DeleteAsync(logoId);

        if (wasCurrent)
        {
            // Chercher un autre logo pour le mettre current (le plus récent)
            IReadOnlyList<ParkLogo> remaining = await parkLogosQueryHandler.GetByParkIdAsync(parkId);
            ParkLogo? newCurrent = remaining.FirstOrDefault();

            if (newCurrent != null)
            {
                newCurrent.IsCurrent = true;
                await parkLogosQueryHandler.UpdateAsync(newCurrent);
                await parkLogosQueryHandler.UnsetCurrentForParkAsync(parkId, newCurrent.Id);
                await parksQueryHandler.UpdateCurrentLogoAsync(parkId, newCurrent.ImageId);
            }
            else
            {
                // plus aucun logo
                await parksQueryHandler.UpdateCurrentLogoAsync(parkId, null);
            }
        }

        return true;
    }

    private static ParkLogoDto ToDto(ParkLogo logo)
    {
        return new ParkLogoDto
        {
            Id = logo.Id,
            ParkId = logo.ParkId,
            ImageId = logo.ImageId,
            Description = logo.Description,
            IsCurrent = logo.IsCurrent,
            CreatedAt = logo.CreatedAt
        };
    }
}