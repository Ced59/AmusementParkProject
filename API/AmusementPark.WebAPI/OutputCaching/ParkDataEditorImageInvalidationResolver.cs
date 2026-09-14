using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.WebAPI.Contracts.Images;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AmusementPark.WebAPI.OutputCaching;

internal static class ParkDataEditorImageInvalidationResolver
{
    private static readonly object PreviousOwnerImpactKey = new object();

    internal static async Task<SsrPageCacheInvalidationRequest> ResolveAsync(
        ActionExecutingContext context,
        ActionExecutedContext? executedContext,
        IImageRepository imageRepository,
        Func<string, string, CancellationToken, Task<SsrPageCacheInvalidationRequest>> resolveOwnerImpact,
        CancellationToken cancellationToken)
    {
        object? argument = SsrInvalidationInputReader.FindActionArgument(context, "request");
        if (argument is ParkDataEditorImageCreateDto)
        {
            // This upload contract creates a new, unattached image. Linking it is
            // a separate mutation, which invalidates the affected public pages.
            return NoPublicImpact();
        }

        if (executedContext is null)
        {
            // The complete impact is returned after execution. Keep the interim
            // result global so the generic filter preserves a failed resolution.
            context.HttpContext.Items[PreviousOwnerImpactKey] = SsrPageCacheInvalidationRequest.AllCaches();
            string? imageId = SsrInvalidationInputReader.GetRouteValue(context, "imageId")
                ?? (argument as LinkImageToOwnerDto)?.ImageId;
            if (string.IsNullOrWhiteSpace(imageId))
            {
                return SsrPageCacheInvalidationRequest.AllCaches();
            }

            Image? previous = await imageRepository.GetByIdAsync(imageId.Trim(), cancellationToken);
            if (previous is null)
            {
                return SsrPageCacheInvalidationRequest.AllCaches();
            }

            SsrPageCacheInvalidationRequest previousImpact = await ResolveOwnerAsync(
                previous.OwnerType, previous.OwnerId, resolveOwnerImpact, cancellationToken);
            context.HttpContext.Items[PreviousOwnerImpactKey] = previousImpact;
            return SsrPageCacheInvalidationRequest.AllCaches();
        }

        if (context.HttpContext.Items[PreviousOwnerImpactKey] is not SsrPageCacheInvalidationRequest previousOwnerImpact
            || previousOwnerImpact.All)
        {
            return SsrPageCacheInvalidationRequest.AllCaches();
        }

        if (SsrInvalidationInputReader.ResolveResultValue(executedContext) is not ImageDto current)
        {
            return SsrPageCacheInvalidationRequest.AllCaches();
        }

        SsrPageCacheInvalidationRequest currentOwnerImpact = await ResolveOwnerAsync(
            (ImageOwnerType)current.OwnerType, current.OwnerId, resolveOwnerImpact, cancellationToken);
        if (currentOwnerImpact.All)
        {
            return currentOwnerImpact;
        }

        return new SsrPageCacheInvalidationRequest
        {
            Paths = previousOwnerImpact.Paths.Concat(currentOwnerImpact.Paths).Distinct(StringComparer.Ordinal).ToList(),
            Prefixes = previousOwnerImpact.Prefixes.Concat(currentOwnerImpact.Prefixes).Distinct(StringComparer.Ordinal).ToList(),
            IncludeSeoDocuments = previousOwnerImpact.IncludeSeoDocuments || currentOwnerImpact.IncludeSeoDocuments,
            AllowStale = previousOwnerImpact.AllowStale && currentOwnerImpact.AllowStale,
            Refresh = false,
        };
    }

    private static async Task<SsrPageCacheInvalidationRequest> ResolveOwnerAsync(
        ImageOwnerType ownerType,
        string? ownerId,
        Func<string, string, CancellationToken, Task<SsrPageCacheInvalidationRequest>> resolveOwnerImpact,
        CancellationToken cancellationToken)
    {
        if (ownerType == ImageOwnerType.None && string.IsNullOrWhiteSpace(ownerId))
        {
            return NoPublicImpact();
        }

        if (ownerType is not (ImageOwnerType.Park or ImageOwnerType.ParkItem or ImageOwnerType.StandaloneAttraction)
            || string.IsNullOrWhiteSpace(ownerId))
        {
            return SsrPageCacheInvalidationRequest.AllCaches();
        }

        SsrPageCacheInvalidationRequest impact = await resolveOwnerImpact(
            ownerType.ToString(), ownerId, cancellationToken);
        if (impact.All || (impact.Paths.Count == 0 && impact.Prefixes.Count == 0))
        {
            return SsrPageCacheInvalidationRequest.AllCaches();
        }

        return new SsrPageCacheInvalidationRequest
        {
            All = impact.All,
            Paths = impact.Paths,
            Prefixes = impact.Prefixes,
            IncludeSeoDocuments = impact.IncludeSeoDocuments,
            AllowStale = false,
            Refresh = false,
        };
    }

    private static SsrPageCacheInvalidationRequest NoPublicImpact()
    {
        return new SsrPageCacheInvalidationRequest { Refresh = false };
    }
}
