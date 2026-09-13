using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.TechnicalPages;

public sealed class TechnicalPagesJsonUpsertDto
{
    public List<TechnicalPageDto> Pages { get; set; } = new();
}
