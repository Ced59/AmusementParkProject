using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.WebAPI.Contracts.Sharing;

namespace AmusementPark.WebAPI.Mappers;

public static class ProfileComparisonInvitationHttpMapper
{
    public static bool TryToApplication(
        this CreateProfileComparisonInvitationRequestDto request,
        string userId,
        out CreateProfileComparisonInvitationCommand? command)
    {
        command = null;
        List<ProfileComparisonCategory> categories = new();
        foreach (string value in request.Categories ?? new List<string>())
        {
            if (!Enum.TryParse(value, true, out ProfileComparisonCategory category)
                || !Enum.IsDefined(category))
            {
                return false;
            }

            categories.Add(category);
        }

        command = new CreateProfileComparisonInvitationCommand(
            userId,
            categories.Distinct().ToArray());
        return true;
    }

    public static ProfileComparisonInvitationCreationDto ToHttp(
        this ProfileComparisonInvitationCreationResult result)
    {
        return new ProfileComparisonInvitationCreationDto
        {
            Token = result.Token,
            ExpiresAtUtc = result.ExpiresAtUtc,
            Categories = result.Categories.Select(static value => value.ToString()).ToList(),
        };
    }

    public static ProfileComparisonInvitationPreviewDto ToHttp(
        this ProfileComparisonInvitationPreviewResult result)
    {
        return new ProfileComparisonInvitationPreviewDto
        {
            Status = result.Status.ToString(),
            CreatorDisplayName = result.CreatorDisplayName,
            InviteeDisplayName = result.InviteeDisplayName,
            ExpiresAtUtc = result.ExpiresAtUtc,
            AcceptedAtUtc = result.AcceptedAtUtc,
            Categories = result.Categories.Select(static value => value.ToString()).ToList(),
            CanAccept = result.CanAccept,
        };
    }

    public static ProfileComparisonInvitationAcceptanceDto ToHttp(
        this ProfileComparisonInvitationAcceptanceResult result)
    {
        return new ProfileComparisonInvitationAcceptanceDto
        {
            ShareId = result.ShareId,
            AcceptedAtUtc = result.AcceptedAtUtc,
            Categories = result.Categories.Select(static value => value.ToString()).ToList(),
        };
    }
}
