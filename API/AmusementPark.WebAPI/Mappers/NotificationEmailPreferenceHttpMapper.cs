using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.WebAPI.Contracts.Watchlists;

namespace AmusementPark.WebAPI.Mappers;

internal static class NotificationEmailPreferenceHttpMapper
{
    internal static NotificationEmailPreferenceDto ToHttp(
        this NotificationEmailPreferenceResult result)
    {
        return new NotificationEmailPreferenceDto(
            result.EmailDigestEnabled,
            result.EmailAvailable,
            result.MaskedEmail,
            result.ConsentTextVersion,
            result.ConsentGrantedAtUtc,
            result.RevokedAtUtc,
            result.Version);
    }

    internal static NotificationEmailPreferenceInput ToApplication(
        this NotificationEmailPreferenceUpdateRequestDto request)
    {
        return new NotificationEmailPreferenceInput(
            request.EmailDigestEnabled,
            request.ConsentAccepted,
            request.ConsentLocale,
            request.ExpectedVersion);
    }
}
