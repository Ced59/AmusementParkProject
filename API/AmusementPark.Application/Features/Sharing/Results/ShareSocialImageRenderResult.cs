namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record ShareSocialImageRenderResult(
    byte[] Content,
    string ContentType,
    string AlternativeText,
    string EntityTag);
