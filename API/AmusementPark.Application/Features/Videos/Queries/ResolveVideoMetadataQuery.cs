using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Videos.Contracts;

namespace AmusementPark.Application.Features.Videos.Queries;

public sealed record ResolveVideoMetadataQuery(string Url) : IQuery<ApplicationResult<ResolvedVideoMetadata>>;
