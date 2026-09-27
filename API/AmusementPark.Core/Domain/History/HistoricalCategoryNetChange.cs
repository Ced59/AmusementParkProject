namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalCategoryNetChange
{
    public HistoricalCategoryNetChange(string category, int fromCount, int toCount)
    {
        string normalizedCategory = category?.Trim() ?? string.Empty;
        if (normalizedCategory.Length == 0 || normalizedCategory.Length > 300)
        {
            throw new ArgumentException("A historical category comparison requires a valid category.", nameof(category));
        }

        if (fromCount < 0 || toCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fromCount));
        }

        this.Category = normalizedCategory;
        this.FromCount = fromCount;
        this.ToCount = toCount;
    }

    public string Category { get; }

    public int FromCount { get; }

    public int ToCount { get; }

    public int NetChange => this.ToCount - this.FromCount;
}
