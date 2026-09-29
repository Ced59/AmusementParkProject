namespace AmusementPark.WebAPI.OutputCaching;

public sealed class PublicLiveCacheGeneration
{
    private long generation;

    public long Current => Interlocked.Read(ref this.generation);

    public long Advance()
    {
        return Interlocked.Increment(ref this.generation);
    }
}
