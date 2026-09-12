using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Seo.Models;

public sealed record PublicSeoContext(string PublicBaseUrl, IReadOnlyCollection<string> SupportedLanguages);
