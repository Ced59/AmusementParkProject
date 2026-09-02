using System.Globalization;
using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.OutputCaching;

/// <summary>
/// Ajoute une génération aux clés du cache de sortie qui exposent des rangs.
/// Une publication rend ainsi inaccessibles les écritures tardives démarrées
/// avec la génération précédente.
/// </summary>
public sealed class RatingRankingGenerationOutputCachePolicy : IOutputCachePolicy
{
    internal const string VaryByKey = "rating-ranking-generation";

    private long generation;

    internal long CurrentGeneration => Volatile.Read(ref this.generation);

    public void Advance()
    {
        Interlocked.Increment(ref this.generation);
    }

    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.CacheVaryByRules.VaryByValues[VaryByKey] = this.CurrentGeneration.ToString(CultureInfo.InvariantCulture);
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
