using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.TechnicalPages;

public sealed class TechnicalPagesJsonUpsertResultDto
{
    public int CreatedCount { get; set; }

    public int UpdatedCount { get; set; }

    public List<TechnicalPageDto> Pages { get; set; } = new();
}
