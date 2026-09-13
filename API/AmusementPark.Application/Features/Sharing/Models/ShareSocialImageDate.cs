using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record ShareSocialImageDate(
    int Year,
    int? Month,
    int? Day,
    ShareDatePrecision Precision);
