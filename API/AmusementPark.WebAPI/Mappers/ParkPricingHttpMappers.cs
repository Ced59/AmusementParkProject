using System.Globalization;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkPricing;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.ParkPricing;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.WebAPI.Mappers;

internal static class ParkPricingHttpMappers
{
    private const string DateFormat = "yyyy-MM-dd";

    public static ApplicationResult<ParkPricingEntity> ToDomainResult(this ParkPricingDto dto, string parkId)
    {
        ArgumentNullException.ThrowIfNull(dto);

        Dictionary<string, List<string>> errors = new(StringComparer.Ordinal);
        ParkPricingEntity pricing = new()
        {
            ParkId = parkId.Trim(),
            CurrencyCode = dto.CurrencyCode?.Trim() ?? string.Empty,
            SourceUrl = NormalizeOptionalString(dto.SourceUrl),
            PurchaseUrl = NormalizeOptionalString(dto.PurchaseUrl),
            Notes = dto.Notes.ToDomain(),
            LastVerifiedAtUtc = dto.LastVerifiedAtUtc,
            AdmissionOffers = (dto.AdmissionOffers ?? Array.Empty<ParkAdmissionPriceOfferDto>())
                .Select((offer, index) => offer.ToDomain(errors, $"admissionOffers[{index}]")).ToList(),
            AnnualPasses = (dto.AnnualPasses ?? Array.Empty<ParkAnnualPassOfferDto>())
                .Select((offer, index) => offer.ToDomain(errors, $"annualPasses[{index}]")).ToList(),
            ParkingOffers = (dto.ParkingOffers ?? Array.Empty<ParkParkingPriceOfferDto>())
                .Select((offer, index) => offer.ToDomain(errors, $"parkingOffers[{index}]")).ToList(),
            CreditOffers = (dto.CreditOffers ?? Array.Empty<ParkCreditOfferDto>())
                .Select((offer, index) => offer.ToDomain(errors, $"creditOffers[{index}]")).ToList(),
            HistoricalSnapshots = (dto.HistoricalSnapshots ?? Array.Empty<ParkPricingSnapshotDto>())
                .Select((snapshot, index) => snapshot.ToDomain(errors, $"historicalSnapshots[{index}]")).ToList(),
        };

        if (errors.Count == 0)
        {
            return ApplicationResult<ParkPricingEntity>.Success(pricing);
        }

        Dictionary<string, IReadOnlyCollection<string>> validationErrors = errors.ToDictionary(
            static item => item.Key,
            static item => (IReadOnlyCollection<string>)item.Value,
            StringComparer.Ordinal);
        return ApplicationResult<ParkPricingEntity>.Failure(ParkPricingApplicationErrors.InvalidPricing(validationErrors));
    }

    public static ParkPricingDto ToHttp(this ParkPricingEntity pricing)
    {
        ArgumentNullException.ThrowIfNull(pricing);

        return new ParkPricingDto
        {
            ParkId = pricing.ParkId,
            CurrencyCode = pricing.CurrencyCode,
            SourceUrl = pricing.SourceUrl,
            PurchaseUrl = pricing.PurchaseUrl,
            Notes = pricing.Notes.ToHttp(),
            LastVerifiedAtUtc = pricing.LastVerifiedAtUtc,
            CreatedAtUtc = pricing.CreatedAtUtc,
            UpdatedAtUtc = pricing.UpdatedAtUtc,
            AdmissionOffers = pricing.AdmissionOffers.Select(static offer => offer.ToHttp()).ToList(),
            AnnualPasses = pricing.AnnualPasses.Select(static offer => offer.ToHttp()).ToList(),
            ParkingOffers = pricing.ParkingOffers.Select(static offer => offer.ToHttp()).ToList(),
            CreditOffers = pricing.CreditOffers.Select(static offer => offer.ToHttp()).ToList(),
            HistoricalSnapshots = pricing.HistoricalSnapshots
                .OrderByDescending(static snapshot => snapshot.Year)
                .Select(static snapshot => snapshot.ToHttp())
                .ToList(),
        };
    }

    public static ParkPricingDto ToPublicHttp(this ParkPricingEntity pricing, int maximumHistoricalSnapshots = 10)
    {
        ParkPricingDto dto = pricing.ToHttp();
        dto.HistoricalSnapshots = (dto.HistoricalSnapshots ?? Array.Empty<ParkPricingSnapshotDto>())
            .OrderByDescending(static snapshot => snapshot.Year)
            .Take(Math.Max(0, maximumHistoricalSnapshots))
            .ToList();
        return dto;
    }

    private static ParkPricingSnapshot ToDomain(
        this ParkPricingSnapshotDto dto,
        Dictionary<string, List<string>> errors,
        string fieldPrefix)
    {
        return new ParkPricingSnapshot
        {
            Id = NormalizeOptionalString(dto.Id),
            Year = dto.Year,
            CurrencyCode = dto.CurrencyCode?.Trim() ?? string.Empty,
            SourceUrl = NormalizeOptionalString(dto.SourceUrl),
            Notes = dto.Notes.ToDomain(),
            LastVerifiedAtUtc = dto.LastVerifiedAtUtc,
            AdmissionOffers = (dto.AdmissionOffers ?? Array.Empty<ParkAdmissionPriceOfferDto>())
                .Select((offer, index) => offer.ToDomain(errors, $"{fieldPrefix}.admissionOffers[{index}]")).ToList(),
            AnnualPasses = (dto.AnnualPasses ?? Array.Empty<ParkAnnualPassOfferDto>())
                .Select((offer, index) => offer.ToDomain(errors, $"{fieldPrefix}.annualPasses[{index}]")).ToList(),
            ParkingOffers = (dto.ParkingOffers ?? Array.Empty<ParkParkingPriceOfferDto>())
                .Select((offer, index) => offer.ToDomain(errors, $"{fieldPrefix}.parkingOffers[{index}]")).ToList(),
            CreditOffers = (dto.CreditOffers ?? Array.Empty<ParkCreditOfferDto>())
                .Select((offer, index) => offer.ToDomain(errors, $"{fieldPrefix}.creditOffers[{index}]")).ToList(),
        };
    }

    private static ParkPricingSnapshotDto ToHttp(this ParkPricingSnapshot snapshot)
    {
        return new ParkPricingSnapshotDto
        {
            Id = snapshot.Id,
            Year = snapshot.Year,
            CurrencyCode = snapshot.CurrencyCode,
            SourceUrl = snapshot.SourceUrl,
            Notes = snapshot.Notes.ToHttp(),
            LastVerifiedAtUtc = snapshot.LastVerifiedAtUtc,
            AdmissionOffers = snapshot.AdmissionOffers.Select(static offer => offer.ToHttp()).ToList(),
            AnnualPasses = snapshot.AnnualPasses.Select(static offer => offer.ToHttp()).ToList(),
            ParkingOffers = snapshot.ParkingOffers.Select(static offer => offer.ToHttp()).ToList(),
            CreditOffers = snapshot.CreditOffers.Select(static offer => offer.ToHttp()).ToList(),
        };
    }

    private static ParkAdmissionPriceOffer ToDomain(this ParkAdmissionPriceOfferDto dto, Dictionary<string, List<string>> errors, string fieldPrefix)
    {
        return new ParkAdmissionPriceOffer
        {
            Id = NormalizeOptionalString(dto.Id),
            Code = dto.Code,
            AudienceCategory = dto.AudienceCategory,
            Labels = dto.Labels.ToDomain(),
            OnlinePrice = dto.OnlinePrice?.ToDomain(errors, $"{fieldPrefix}.onlinePrice"),
            GatePrice = dto.GatePrice?.ToDomain(errors, $"{fieldPrefix}.gatePrice"),
            ValidFrom = ParseOptionalDate(dto.ValidFrom, errors, $"{fieldPrefix}.validFrom"),
            ValidTo = ParseOptionalDate(dto.ValidTo, errors, $"{fieldPrefix}.validTo"),
            PurchaseUrl = NormalizeOptionalString(dto.PurchaseUrl),
            Conditions = dto.Conditions.ToDomain(),
            SortOrder = dto.SortOrder,
        };
    }

    private static ParkAnnualPassOffer ToDomain(this ParkAnnualPassOfferDto dto, Dictionary<string, List<string>> errors, string fieldPrefix)
    {
        return new ParkAnnualPassOffer
        {
            Id = NormalizeOptionalString(dto.Id),
            Code = dto.Code,
            Names = dto.Names.ToDomain(),
            OnlinePrice = dto.OnlinePrice?.ToDomain(errors, $"{fieldPrefix}.onlinePrice"),
            GatePrice = dto.GatePrice?.ToDomain(errors, $"{fieldPrefix}.gatePrice"),
            ValidFrom = ParseOptionalDate(dto.ValidFrom, errors, $"{fieldPrefix}.validFrom"),
            ValidTo = ParseOptionalDate(dto.ValidTo, errors, $"{fieldPrefix}.validTo"),
            PurchaseUrl = NormalizeOptionalString(dto.PurchaseUrl),
            Conditions = dto.Conditions.ToDomain(),
            SortOrder = dto.SortOrder,
        };
    }

    private static ParkParkingPriceOffer ToDomain(this ParkParkingPriceOfferDto dto, Dictionary<string, List<string>> errors, string fieldPrefix)
    {
        return new ParkParkingPriceOffer
        {
            Id = NormalizeOptionalString(dto.Id),
            Code = dto.Code,
            Labels = dto.Labels.ToDomain(),
            OnlinePrice = dto.OnlinePrice?.ToDomain(errors, $"{fieldPrefix}.onlinePrice"),
            GatePrice = dto.GatePrice?.ToDomain(errors, $"{fieldPrefix}.gatePrice"),
            ValidFrom = ParseOptionalDate(dto.ValidFrom, errors, $"{fieldPrefix}.validFrom"),
            ValidTo = ParseOptionalDate(dto.ValidTo, errors, $"{fieldPrefix}.validTo"),
            PurchaseUrl = NormalizeOptionalString(dto.PurchaseUrl),
            Conditions = dto.Conditions.ToDomain(),
            SortOrder = dto.SortOrder,
        };
    }

    private static ParkCreditOffer ToDomain(this ParkCreditOfferDto dto, Dictionary<string, List<string>> errors, string fieldPrefix)
    {
        return new ParkCreditOffer
        {
            Id = NormalizeOptionalString(dto.Id),
            UnitCode = dto.UnitCode,
            Quantity = dto.Quantity,
            Labels = dto.Labels.ToDomain(),
            Prices = new ParkCreditOfferPrices
            {
                OnlinePrice = dto.Prices?.OnlinePrice,
                GatePrice = dto.Prices?.GatePrice,
            },
            ValidFrom = ParseOptionalDate(dto.ValidFrom, errors, $"{fieldPrefix}.validFrom"),
            ValidTo = ParseOptionalDate(dto.ValidTo, errors, $"{fieldPrefix}.validTo"),
            PurchaseUrl = NormalizeOptionalString(dto.PurchaseUrl),
            Conditions = dto.Conditions.ToDomain(),
            SortOrder = dto.SortOrder,
        };
    }

    private static ParkPriceValue ToDomain(this ParkPriceValueDto dto, Dictionary<string, List<string>> errors, string fieldPrefix)
    {
        ParkPricingMode mode = ParkPricingMode.Fixed;
        if (!Enum.TryParse(dto.Mode, true, out mode) || !Enum.IsDefined(mode))
        {
            AddError(errors, $"{fieldPrefix}.mode", "Invalid pricing mode. Allowed values are Fixed, Range and Dynamic.");
            mode = ParkPricingMode.Fixed;
        }

        return new ParkPriceValue
        {
            Mode = mode,
            Amount = dto.Amount,
            MinimumAmount = dto.MinimumAmount,
            MaximumAmount = dto.MaximumAmount,
        };
    }

    private static ParkAdmissionPriceOfferDto ToHttp(this ParkAdmissionPriceOffer offer)
    {
        return new ParkAdmissionPriceOfferDto
        {
            Id = offer.Id,
            Code = offer.Code,
            AudienceCategory = offer.AudienceCategory,
            Labels = offer.Labels.ToHttp(),
            OnlinePrice = offer.OnlinePrice?.ToHttp(),
            GatePrice = offer.GatePrice?.ToHttp(),
            ValidFrom = FormatDate(offer.ValidFrom),
            ValidTo = FormatDate(offer.ValidTo),
            PurchaseUrl = offer.PurchaseUrl,
            Conditions = offer.Conditions.ToHttp(),
            SortOrder = offer.SortOrder,
        };
    }

    private static ParkAnnualPassOfferDto ToHttp(this ParkAnnualPassOffer offer)
    {
        return new ParkAnnualPassOfferDto
        {
            Id = offer.Id,
            Code = offer.Code,
            Names = offer.Names.ToHttp(),
            OnlinePrice = offer.OnlinePrice?.ToHttp(),
            GatePrice = offer.GatePrice?.ToHttp(),
            ValidFrom = FormatDate(offer.ValidFrom),
            ValidTo = FormatDate(offer.ValidTo),
            PurchaseUrl = offer.PurchaseUrl,
            Conditions = offer.Conditions.ToHttp(),
            SortOrder = offer.SortOrder,
        };
    }

    private static ParkParkingPriceOfferDto ToHttp(this ParkParkingPriceOffer offer)
    {
        return new ParkParkingPriceOfferDto
        {
            Id = offer.Id,
            Code = offer.Code,
            Labels = offer.Labels.ToHttp(),
            OnlinePrice = offer.OnlinePrice?.ToHttp(),
            GatePrice = offer.GatePrice?.ToHttp(),
            ValidFrom = FormatDate(offer.ValidFrom),
            ValidTo = FormatDate(offer.ValidTo),
            PurchaseUrl = offer.PurchaseUrl,
            Conditions = offer.Conditions.ToHttp(),
            SortOrder = offer.SortOrder,
        };
    }

    private static ParkCreditOfferDto ToHttp(this ParkCreditOffer offer)
    {
        return new ParkCreditOfferDto
        {
            Id = offer.Id,
            UnitCode = offer.UnitCode,
            Quantity = offer.Quantity,
            Labels = offer.Labels.ToHttp(),
            Prices = new ParkCreditOfferPricesDto
            {
                OnlinePrice = offer.Prices.OnlinePrice,
                GatePrice = offer.Prices.GatePrice,
            },
            ValidFrom = FormatDate(offer.ValidFrom),
            ValidTo = FormatDate(offer.ValidTo),
            PurchaseUrl = offer.PurchaseUrl,
            Conditions = offer.Conditions.ToHttp(),
            SortOrder = offer.SortOrder,
        };
    }

    private static ParkPriceValueDto ToHttp(this ParkPriceValue price)
    {
        return new ParkPriceValueDto
        {
            Mode = price.Mode.ToString(),
            Amount = price.Amount,
            MinimumAmount = price.MinimumAmount,
            MaximumAmount = price.MaximumAmount,
        };
    }

    private static DateOnly? ParseOptionalDate(string? value, Dictionary<string, List<string>> errors, string fieldPath)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParseExact(value.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed))
        {
            return parsed;
        }

        AddError(errors, fieldPath, $"Date must use the {DateFormat} format.");
        return null;
    }

    private static string? FormatDate(DateOnly? value)
    {
        return value?.ToString(DateFormat, CultureInfo.InvariantCulture);
    }

    private static void AddError(Dictionary<string, List<string>> errors, string fieldPath, string message)
    {
        if (!errors.TryGetValue(fieldPath, out List<string>? messages))
        {
            messages = new List<string>();
            errors[fieldPath] = messages;
        }

        messages.Add(message);
    }

    private static string? NormalizeOptionalString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
