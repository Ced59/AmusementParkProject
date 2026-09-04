namespace AmusementPark.Application.Features.Passport.Models;

public sealed class PassportExportSourceBudget
{
    public PassportExportSourceBudget(long maximumBytes)
    {
        if (maximumBytes < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        }

        this.MaximumBytes = maximumBytes;
    }

    public long MaximumBytes { get; }

    public long ConsumedBytes { get; private set; }

    public bool TryConsume(long bytes)
    {
        if (bytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes));
        }

        if (bytes > this.MaximumBytes - this.ConsumedBytes)
        {
            return false;
        }

        this.ConsumedBytes += bytes;
        return true;
    }
}
