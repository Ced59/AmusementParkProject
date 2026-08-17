from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def write(path: str, content: str) -> None:
    target = ROOT / path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(content, encoding="utf-8")


def replace(path: str, old: str, new: str, count: int = 1) -> None:
    content = read(path)
    occurrences = content.count(old)
    if occurrences < count:
        raise RuntimeError(f"Anchor not found in {path}: expected >= {count}, got {occurrences}: {old[:120]!r}")
    write(path, content.replace(old, new, count))


# -------------------------
# Domain
# -------------------------
path = "API/AmusementPark.Core/Domain/Parks/ParkPricing.cs"
replace(path,
"    public List<ParkParkingPriceOffer> ParkingOffers { get; set; } = new();\n\n    public List<ParkPricingSnapshot> HistoricalSnapshots { get; set; } = new();",
"    public List<ParkParkingPriceOffer> ParkingOffers { get; set; } = new();\n\n    public List<ParkCreditOffer> CreditOffers { get; set; } = new();\n\n    public List<ParkPricingSnapshot> HistoricalSnapshots { get; set; } = new();",
2)
replace(path,
"            ParkingOffers = this.ParkingOffers.Where(offer => IsValidOn(offer.ValidFrom, offer.ValidTo, date)).ToList(),\n            HistoricalSnapshots = this.HistoricalSnapshots.ToList(),",
"            ParkingOffers = this.ParkingOffers.Where(offer => IsValidOn(offer.ValidFrom, offer.ValidTo, date)).ToList(),\n            CreditOffers = this.CreditOffers.Where(offer => IsValidOn(offer.ValidFrom, offer.ValidTo, date)).ToList(),\n            HistoricalSnapshots = this.HistoricalSnapshots.ToList(),")
replace(path,
"            || this.ParkingOffers.Any(offer => (offer.OnlinePrice is not null || offer.GatePrice is not null) && IsValidOn(offer.ValidFrom, offer.ValidTo, date));",
"            || this.ParkingOffers.Any(offer => (offer.OnlinePrice is not null || offer.GatePrice is not null) && IsValidOn(offer.ValidFrom, offer.ValidTo, date))\n            || this.CreditOffers.Any(offer => (offer.Prices.OnlinePrice.HasValue || offer.Prices.GatePrice.HasValue) && IsValidOn(offer.ValidFrom, offer.ValidTo, date));")
replace(path,
"        return this.AdmissionOffers.Any(static offer => offer.OnlinePrice is not null || offer.GatePrice is not null)\n            || this.AnnualPasses.Any(static offer => offer.OnlinePrice is not null || offer.GatePrice is not null)\n            || this.ParkingOffers.Any(static offer => offer.OnlinePrice is not null || offer.GatePrice is not null);",
"        return this.AdmissionOffers.Any(static offer => offer.OnlinePrice is not null || offer.GatePrice is not null)\n            || this.AnnualPasses.Any(static offer => offer.OnlinePrice is not null || offer.GatePrice is not null)\n            || this.ParkingOffers.Any(static offer => offer.OnlinePrice is not null || offer.GatePrice is not null)\n            || this.CreditOffers.Any(static offer => offer.Prices.OnlinePrice.HasValue || offer.Prices.GatePrice.HasValue);")
replace(path,
"public sealed class ParkPriceValue\n{",
"public sealed class ParkCreditOffer\n{\n    public string? Id { get; set; }\n\n    public string UnitCode { get; set; } = string.Empty;\n\n    public int Quantity { get; set; }\n\n    public List<LocalizedText> Labels { get; set; } = new();\n\n    public ParkCreditOfferPrices Prices { get; set; } = new();\n\n    public DateOnly? ValidFrom { get; set; }\n\n    public DateOnly? ValidTo { get; set; }\n\n    public string? PurchaseUrl { get; set; }\n\n    public List<LocalizedText> Conditions { get; set; } = new();\n\n    public int SortOrder { get; set; }\n}\n\npublic sealed class ParkCreditOfferPrices\n{\n    public decimal? OnlinePrice { get; set; }\n\n    public decimal? GatePrice { get; set; }\n}\n\npublic sealed class ParkPriceValue\n{")

# -------------------------
# Application command preservation
# -------------------------
path = "API/AmusementPark.Application/Features/ParkPricing/Commands/ParkPricingCommands.cs"
replace(path,
"public sealed record UpsertParkPricingCommand(\n    ParkPricingEntity Pricing,\n    bool PreserveHistoricalSnapshots = false) : ICommand<ApplicationResult<ParkPricingEntity>>;",
"public sealed record UpsertParkPricingCommand(\n    ParkPricingEntity Pricing,\n    bool PreserveHistoricalSnapshots = false,\n    bool PreserveCreditOffers = false) : ICommand<ApplicationResult<ParkPricingEntity>>;")

path = "API/AmusementPark.Application/Features/ParkPricing/Handlers/ParkPricingCommandHandlers.cs"
replace(path,
"        if (command.PreserveHistoricalSnapshots && !string.IsNullOrWhiteSpace(command.Pricing.ParkId))\n        {\n            ParkPricingEntity? existingPricing = await this.pricingRepository.GetByParkIdAsync(\n                command.Pricing.ParkId.Trim(),\n                cancellationToken);\n            if (existingPricing is not null)\n            {\n                command.Pricing.HistoricalSnapshots = existingPricing.HistoricalSnapshots;\n            }\n        }",
"        if ((command.PreserveHistoricalSnapshots || command.PreserveCreditOffers)\n            && !string.IsNullOrWhiteSpace(command.Pricing.ParkId))\n        {\n            ParkPricingEntity? existingPricing = await this.pricingRepository.GetByParkIdAsync(\n                command.Pricing.ParkId.Trim(),\n                cancellationToken);\n            if (existingPricing is not null)\n            {\n                if (command.PreserveHistoricalSnapshots)\n                {\n                    command.Pricing.HistoricalSnapshots = existingPricing.HistoricalSnapshots;\n                }\n\n                if (command.PreserveCreditOffers)\n                {\n                    command.Pricing.CreditOffers = existingPricing.CreditOffers;\n                }\n            }\n        }")

# -------------------------
# Normalizer
# -------------------------
path = "API/AmusementPark.Application/Features/ParkPricing/Services/ParkPricingNormalizer.cs"
replace(path,
"    private const int MaximumParkingOfferCount = 50;",
"    private const int MaximumParkingOfferCount = 50;\n    private const int MaximumCreditOfferCount = 100;")
replace(path,
"        IReadOnlyCollection<ParkParkingPriceOffer> parkingOffers = pricing.ParkingOffers ?? new List<ParkParkingPriceOffer>();",
"        IReadOnlyCollection<ParkParkingPriceOffer> parkingOffers = pricing.ParkingOffers ?? new List<ParkParkingPriceOffer>();\n        IReadOnlyCollection<ParkCreditOffer> creditOffers = pricing.CreditOffers ?? new List<ParkCreditOffer>();")
replace(path,
"        if (parkingOffers.Count > MaximumParkingOfferCount)\n        {\n            errors[nameof(pricing.ParkingOffers)] = new[] { \"too-many-offers\" };\n        }",
"        if (parkingOffers.Count > MaximumParkingOfferCount)\n        {\n            errors[nameof(pricing.ParkingOffers)] = new[] { \"too-many-offers\" };\n        }\n\n        if (creditOffers.Count > MaximumCreditOfferCount)\n        {\n            errors[nameof(pricing.CreditOffers)] = new[] { \"too-many-offers\" };\n        }")
replace(path,
"        normalized.ParkingOffers = NormalizeParkingOffers(parkingOffers, nameof(pricing.ParkingOffers), errors);\n        normalized.HistoricalSnapshots = NormalizeHistoricalSnapshots(",
"        normalized.ParkingOffers = NormalizeParkingOffers(parkingOffers, nameof(pricing.ParkingOffers), errors);\n        normalized.CreditOffers = NormalizeCreditOffers(creditOffers, nameof(pricing.CreditOffers), errors);\n        normalized.HistoricalSnapshots = NormalizeHistoricalSnapshots(")
replace(path,
"            || pricing.ParkingOffers.Any(static offer => offer.OnlinePrice is not null || offer.GatePrice is not null);",
"            || pricing.ParkingOffers.Any(static offer => offer.OnlinePrice is not null || offer.GatePrice is not null)\n            || pricing.CreditOffers.Any(static offer => offer.Prices.OnlinePrice.HasValue || offer.Prices.GatePrice.HasValue);")
replace(path,
"            IReadOnlyCollection<ParkParkingPriceOffer> parkingOffers = snapshot.ParkingOffers ?? new List<ParkParkingPriceOffer>();",
"            IReadOnlyCollection<ParkParkingPriceOffer> parkingOffers = snapshot.ParkingOffers ?? new List<ParkParkingPriceOffer>();\n            IReadOnlyCollection<ParkCreditOffer> creditOffers = snapshot.CreditOffers ?? new List<ParkCreditOffer>();")
replace(path,
"                ParkingOffers = NormalizeParkingOffers(parkingOffers, $\"{fieldPrefix}.ParkingOffers\", errors),\n            };",
"                ParkingOffers = NormalizeParkingOffers(parkingOffers, $\"{fieldPrefix}.ParkingOffers\", errors),\n                CreditOffers = NormalizeCreditOffers(creditOffers, $\"{fieldPrefix}.CreditOffers\", errors),\n            };")
replace(path,
"        if (snapshot.ParkingOffers.Count > MaximumParkingOfferCount)\n        {\n            errors[$\"{fieldPrefix}.ParkingOffers\"] = new[] { \"too-many-offers\" };\n        }",
"        if (snapshot.ParkingOffers.Count > MaximumParkingOfferCount)\n        {\n            errors[$\"{fieldPrefix}.ParkingOffers\"] = new[] { \"too-many-offers\" };\n        }\n\n        if (snapshot.CreditOffers.Count > MaximumCreditOfferCount)\n        {\n            errors[$\"{fieldPrefix}.CreditOffers\"] = new[] { \"too-many-offers\" };\n        }")
replace(path,
"    private static ParkPriceValue? NormalizePrice(\n",
"    private static List<ParkCreditOffer> NormalizeCreditOffers(\n        IReadOnlyCollection<ParkCreditOffer> offers,\n        string collectionFieldPath,\n        Dictionary<string, IReadOnlyCollection<string>> errors)\n    {\n        HashSet<string> usedProducts = new(StringComparer.OrdinalIgnoreCase);\n        List<ParkCreditOffer> normalizedOffers = new();\n        int index = 0;\n\n        foreach (ParkCreditOffer offer in offers)\n        {\n            string fieldPrefix = $\"{collectionFieldPath}[{index}]\";\n            ParkCreditOfferPrices prices = offer.Prices ?? new ParkCreditOfferPrices();\n            ParkCreditOffer normalized = new()\n            {\n                Id = NormalizeOptionalString(offer.Id) ?? Guid.NewGuid().ToString(\"N\"),\n                UnitCode = NormalizeCode(offer.UnitCode),\n                Quantity = offer.Quantity,\n                Labels = NormalizeLocalizedTexts(offer.Labels),\n                Prices = new ParkCreditOfferPrices\n                {\n                    OnlinePrice = prices.OnlinePrice,\n                    GatePrice = prices.GatePrice,\n                },\n                ValidFrom = offer.ValidFrom,\n                ValidTo = offer.ValidTo,\n                PurchaseUrl = NormalizeOptionalString(offer.PurchaseUrl),\n                Conditions = NormalizeLocalizedTexts(offer.Conditions),\n                SortOrder = offer.SortOrder > 0 ? offer.SortOrder : index + 1,\n            };\n\n            if (string.IsNullOrWhiteSpace(normalized.UnitCode))\n            {\n                errors[$\"{fieldPrefix}.unitCode\"] = new[] { \"required\" };\n            }\n\n            if (normalized.Quantity <= 0)\n            {\n                errors[$\"{fieldPrefix}.quantity\"] = new[] { \"positive-required\" };\n            }\n\n            string productKey = $\"{normalized.UnitCode}:{normalized.Quantity}\";\n            if (!string.IsNullOrWhiteSpace(normalized.UnitCode) && normalized.Quantity > 0 && !usedProducts.Add(productKey))\n            {\n                errors[$\"{fieldPrefix}.quantity\"] = new[] { \"duplicate\" };\n            }\n\n            ValidateRequiredLocalizedTexts(normalized.Labels, $\"{fieldPrefix}.labels\", errors);\n            if (!normalized.Prices.OnlinePrice.HasValue && !normalized.Prices.GatePrice.HasValue)\n            {\n                errors[$\"{fieldPrefix}.prices\"] = new[] { \"price-required\" };\n            }\n\n            if (normalized.Prices.OnlinePrice < 0)\n            {\n                errors[$\"{fieldPrefix}.prices.onlinePrice\"] = new[] { \"negative-price\" };\n            }\n\n            if (normalized.Prices.GatePrice < 0)\n            {\n                errors[$\"{fieldPrefix}.prices.gatePrice\"] = new[] { \"negative-price\" };\n            }\n\n            ValidateDateRange(normalized.ValidFrom, normalized.ValidTo, fieldPrefix, errors);\n            ValidateOptionalAbsoluteHttpUrl(normalized.PurchaseUrl, $\"{fieldPrefix}.purchaseUrl\", errors);\n            ValidateOptionalLocalizedTexts(normalized.Conditions, $\"{fieldPrefix}.conditions\", errors);\n            normalizedOffers.Add(normalized);\n            index += 1;\n        }\n\n        return normalizedOffers\n            .OrderBy(static item => item.SortOrder)\n            .ThenBy(static item => item.UnitCode, StringComparer.Ordinal)\n            .ThenBy(static item => item.Quantity)\n            .ToList();\n    }\n\n    private static ParkPriceValue? NormalizePrice(\n")

# -------------------------
# Mongo documents + mapper
# -------------------------
path = "API/AmusementPark.Infrastructure/Persistence/Mongo/Documents/ParkPricing/ParkPricingDocuments.cs"
replace(path,
"    [BsonElement(\"parkingOffers\")]\n    public List<ParkParkingPriceOfferDocument> ParkingOffers { get; set; } = new();\n\n    [BsonElement(\"historicalSnapshots\")]",
"    [BsonElement(\"parkingOffers\")]\n    public List<ParkParkingPriceOfferDocument> ParkingOffers { get; set; } = new();\n\n    [BsonElement(\"creditOffers\")]\n    public List<ParkCreditOfferDocument> CreditOffers { get; set; } = new();\n\n    [BsonElement(\"historicalSnapshots\")]",
1)
replace(path,
"    [BsonElement(\"parkingOffers\")]\n    public List<ParkParkingPriceOfferDocument> ParkingOffers { get; set; } = new();\n}\n\n[BsonIgnoreExtraElements]\npublic sealed class ParkAdmissionPriceOfferDocument",
"    [BsonElement(\"parkingOffers\")]\n    public List<ParkParkingPriceOfferDocument> ParkingOffers { get; set; } = new();\n\n    [BsonElement(\"creditOffers\")]\n    public List<ParkCreditOfferDocument> CreditOffers { get; set; } = new();\n}\n\n[BsonIgnoreExtraElements]\npublic sealed class ParkAdmissionPriceOfferDocument")
replace(path,
"[BsonIgnoreExtraElements]\npublic sealed class ParkPriceValueDocument",
"[BsonIgnoreExtraElements]\npublic sealed class ParkCreditOfferDocument\n{\n    [BsonElement(\"id\")]\n    public string Id { get; set; } = string.Empty;\n\n    [BsonElement(\"unitCode\")]\n    public string UnitCode { get; set; } = string.Empty;\n\n    [BsonElement(\"quantity\")]\n    public int Quantity { get; set; }\n\n    [BsonElement(\"labels\")]\n    public List<LocalizedTextDocument> Labels { get; set; } = new();\n\n    [BsonElement(\"prices\")]\n    public ParkCreditOfferPricesDocument Prices { get; set; } = new();\n\n    [BsonElement(\"validFrom\")]\n    [BsonIgnoreIfNull]\n    public string? ValidFrom { get; set; }\n\n    [BsonElement(\"validTo\")]\n    [BsonIgnoreIfNull]\n    public string? ValidTo { get; set; }\n\n    [BsonElement(\"purchaseUrl\")]\n    [BsonIgnoreIfNull]\n    public string? PurchaseUrl { get; set; }\n\n    [BsonElement(\"conditions\")]\n    public List<LocalizedTextDocument> Conditions { get; set; } = new();\n\n    [BsonElement(\"sortOrder\")]\n    public int SortOrder { get; set; }\n}\n\n[BsonIgnoreExtraElements]\npublic sealed class ParkCreditOfferPricesDocument\n{\n    [BsonElement(\"onlinePrice\")]\n    [BsonIgnoreIfNull]\n    public decimal? OnlinePrice { get; set; }\n\n    [BsonElement(\"gatePrice\")]\n    [BsonIgnoreIfNull]\n    public decimal? GatePrice { get; set; }\n}\n\n[BsonIgnoreExtraElements]\npublic sealed class ParkPriceValueDocument")

path = "API/AmusementPark.Infrastructure/Persistence/Mongo/Mappers/EntityMongoMappers.ParkPricing.cs"
replace(path,
"            ParkingOffers = pricing.ParkingOffers.Select(static offer => offer.ToDocument()).ToList(),\n            HistoricalSnapshots = pricing.HistoricalSnapshots",
"            ParkingOffers = pricing.ParkingOffers.Select(static offer => offer.ToDocument()).ToList(),\n            CreditOffers = pricing.CreditOffers.Select(static offer => offer.ToDocument()).ToList(),\n            HistoricalSnapshots = pricing.HistoricalSnapshots")
replace(path,
"            ParkingOffers = document.ParkingOffers.Select(static offer => offer.ToDomain()).ToList(),\n            HistoricalSnapshots = document.HistoricalSnapshots",
"            ParkingOffers = document.ParkingOffers.Select(static offer => offer.ToDomain()).ToList(),\n            CreditOffers = document.CreditOffers.Select(static offer => offer.ToDomain()).ToList(),\n            HistoricalSnapshots = document.HistoricalSnapshots")
replace(path,
"            ParkingOffers = snapshot.ParkingOffers.Select(static offer => offer.ToDocument()).ToList(),\n        };",
"            ParkingOffers = snapshot.ParkingOffers.Select(static offer => offer.ToDocument()).ToList(),\n            CreditOffers = snapshot.CreditOffers.Select(static offer => offer.ToDocument()).ToList(),\n        };")
replace(path,
"            ParkingOffers = document.ParkingOffers.Select(static offer => offer.ToDomain()).ToList(),\n        };",
"            ParkingOffers = document.ParkingOffers.Select(static offer => offer.ToDomain()).ToList(),\n            CreditOffers = document.CreditOffers.Select(static offer => offer.ToDomain()).ToList(),\n        };")
replace(path,
"    private static ParkPriceValueDocument ToDocument(this ParkPriceValue price)",
"    private static ParkCreditOfferDocument ToDocument(this ParkCreditOffer offer)\n    {\n        return new ParkCreditOfferDocument\n        {\n            Id = string.IsNullOrWhiteSpace(offer.Id) ? Guid.NewGuid().ToString(\"N\") : offer.Id,\n            UnitCode = offer.UnitCode,\n            Quantity = offer.Quantity,\n            Labels = CommonMongoMappers.ToDocuments(offer.Labels),\n            Prices = new ParkCreditOfferPricesDocument\n            {\n                OnlinePrice = offer.Prices.OnlinePrice,\n                GatePrice = offer.Prices.GatePrice,\n            },\n            ValidFrom = FormatPricingDate(offer.ValidFrom),\n            ValidTo = FormatPricingDate(offer.ValidTo),\n            PurchaseUrl = offer.PurchaseUrl,\n            Conditions = CommonMongoMappers.ToDocuments(offer.Conditions),\n            SortOrder = offer.SortOrder,\n        };\n    }\n\n    private static ParkCreditOffer ToDomain(this ParkCreditOfferDocument document)\n    {\n        return new ParkCreditOffer\n        {\n            Id = document.Id,\n            UnitCode = document.UnitCode,\n            Quantity = document.Quantity,\n            Labels = CommonMongoMappers.ToDomain(document.Labels),\n            Prices = new ParkCreditOfferPrices\n            {\n                OnlinePrice = document.Prices?.OnlinePrice,\n                GatePrice = document.Prices?.GatePrice,\n            },\n            ValidFrom = ParsePricingDate(document.ValidFrom),\n            ValidTo = ParsePricingDate(document.ValidTo),\n            PurchaseUrl = document.PurchaseUrl,\n            Conditions = CommonMongoMappers.ToDomain(document.Conditions),\n            SortOrder = document.SortOrder,\n        };\n    }\n\n    private static ParkPriceValueDocument ToDocument(this ParkPriceValue price)")

# -------------------------
# Web API contract + mapping + preservation flag
# -------------------------
path = "API/AmusementPark.WebAPI/Contracts/ParkPricing/ParkPricingDtos.cs"
replace(path,
"    public IReadOnlyCollection<ParkParkingPriceOfferDto> ParkingOffers { get; set; } = Array.Empty<ParkParkingPriceOfferDto>();\n\n    public IReadOnlyCollection<ParkPricingSnapshotDto>? HistoricalSnapshots { get; set; }",
"    public IReadOnlyCollection<ParkParkingPriceOfferDto> ParkingOffers { get; set; } = Array.Empty<ParkParkingPriceOfferDto>();\n\n    public IReadOnlyCollection<ParkCreditOfferDto>? CreditOffers { get; set; }\n\n    public IReadOnlyCollection<ParkPricingSnapshotDto>? HistoricalSnapshots { get; set; }")
replace(path,
"    public IReadOnlyCollection<ParkParkingPriceOfferDto> ParkingOffers { get; set; } = Array.Empty<ParkParkingPriceOfferDto>();\n}\n\npublic sealed class ParkAdmissionPriceOfferDto",
"    public IReadOnlyCollection<ParkParkingPriceOfferDto> ParkingOffers { get; set; } = Array.Empty<ParkParkingPriceOfferDto>();\n\n    public IReadOnlyCollection<ParkCreditOfferDto> CreditOffers { get; set; } = Array.Empty<ParkCreditOfferDto>();\n}\n\npublic sealed class ParkAdmissionPriceOfferDto")
replace(path,
"public sealed class ParkPriceValueDto\n{",
"public sealed class ParkCreditOfferDto\n{\n    public string? Id { get; set; }\n\n    public string UnitCode { get; set; } = string.Empty;\n\n    public int Quantity { get; set; }\n\n    public IReadOnlyCollection<LocalizedTextDto> Labels { get; set; } = Array.Empty<LocalizedTextDto>();\n\n    public ParkCreditOfferPricesDto? Prices { get; set; }\n\n    public string? ValidFrom { get; set; }\n\n    public string? ValidTo { get; set; }\n\n    public string? PurchaseUrl { get; set; }\n\n    public IReadOnlyCollection<LocalizedTextDto> Conditions { get; set; } = Array.Empty<LocalizedTextDto>();\n\n    public int SortOrder { get; set; }\n}\n\npublic sealed class ParkCreditOfferPricesDto\n{\n    public decimal? OnlinePrice { get; set; }\n\n    public decimal? GatePrice { get; set; }\n}\n\npublic sealed class ParkPriceValueDto\n{")

path = "API/AmusementPark.WebAPI/Mappers/ParkPricingHttpMappers.cs"
replace(path,
"            ParkingOffers = (dto.ParkingOffers ?? Array.Empty<ParkParkingPriceOfferDto>())\n                .Select((offer, index) => offer.ToDomain(errors, $\"parkingOffers[{index}]\")).ToList(),\n            HistoricalSnapshots =",
"            ParkingOffers = (dto.ParkingOffers ?? Array.Empty<ParkParkingPriceOfferDto>())\n                .Select((offer, index) => offer.ToDomain(errors, $\"parkingOffers[{index}]\")).ToList(),\n            CreditOffers = (dto.CreditOffers ?? Array.Empty<ParkCreditOfferDto>())\n                .Select((offer, index) => offer.ToDomain(errors, $\"creditOffers[{index}]\")).ToList(),\n            HistoricalSnapshots =")
replace(path,
"            ParkingOffers = pricing.ParkingOffers.Select(static offer => offer.ToHttp()).ToList(),\n            HistoricalSnapshots =",
"            ParkingOffers = pricing.ParkingOffers.Select(static offer => offer.ToHttp()).ToList(),\n            CreditOffers = pricing.CreditOffers.Select(static offer => offer.ToHttp()).ToList(),\n            HistoricalSnapshots =")
replace(path,
"            ParkingOffers = (dto.ParkingOffers ?? Array.Empty<ParkParkingPriceOfferDto>())\n                .Select((offer, index) => offer.ToDomain(errors, $\"{fieldPrefix}.parkingOffers[{index}]\")).ToList(),\n        };",
"            ParkingOffers = (dto.ParkingOffers ?? Array.Empty<ParkParkingPriceOfferDto>())\n                .Select((offer, index) => offer.ToDomain(errors, $\"{fieldPrefix}.parkingOffers[{index}]\")).ToList(),\n            CreditOffers = (dto.CreditOffers ?? Array.Empty<ParkCreditOfferDto>())\n                .Select((offer, index) => offer.ToDomain(errors, $\"{fieldPrefix}.creditOffers[{index}]\")).ToList(),\n        };")
replace(path,
"            ParkingOffers = snapshot.ParkingOffers.Select(static offer => offer.ToHttp()).ToList(),\n        };",
"            ParkingOffers = snapshot.ParkingOffers.Select(static offer => offer.ToHttp()).ToList(),\n            CreditOffers = snapshot.CreditOffers.Select(static offer => offer.ToHttp()).ToList(),\n        };")
replace(path,
"    private static ParkPriceValue ToDomain(this ParkPriceValueDto dto, Dictionary<string, List<string>> errors, string fieldPrefix)",
"    private static ParkCreditOffer ToDomain(this ParkCreditOfferDto dto, Dictionary<string, List<string>> errors, string fieldPrefix)\n    {\n        return new ParkCreditOffer\n        {\n            Id = NormalizeOptionalString(dto.Id),\n            UnitCode = dto.UnitCode,\n            Quantity = dto.Quantity,\n            Labels = dto.Labels.ToDomain(),\n            Prices = new ParkCreditOfferPrices\n            {\n                OnlinePrice = dto.Prices?.OnlinePrice,\n                GatePrice = dto.Prices?.GatePrice,\n            },\n            ValidFrom = ParseOptionalDate(dto.ValidFrom, errors, $\"{fieldPrefix}.validFrom\"),\n            ValidTo = ParseOptionalDate(dto.ValidTo, errors, $\"{fieldPrefix}.validTo\"),\n            PurchaseUrl = NormalizeOptionalString(dto.PurchaseUrl),\n            Conditions = dto.Conditions.ToDomain(),\n            SortOrder = dto.SortOrder,\n        };\n    }\n\n    private static ParkPriceValue ToDomain(this ParkPriceValueDto dto, Dictionary<string, List<string>> errors, string fieldPrefix)")
replace(path,
"    private static ParkPriceValueDto ToHttp(this ParkPriceValue price)",
"    private static ParkCreditOfferDto ToHttp(this ParkCreditOffer offer)\n    {\n        return new ParkCreditOfferDto\n        {\n            Id = offer.Id,\n            UnitCode = offer.UnitCode,\n            Quantity = offer.Quantity,\n            Labels = offer.Labels.ToHttp(),\n            Prices = new ParkCreditOfferPricesDto\n            {\n                OnlinePrice = offer.Prices.OnlinePrice,\n                GatePrice = offer.Prices.GatePrice,\n            },\n            ValidFrom = FormatDate(offer.ValidFrom),\n            ValidTo = FormatDate(offer.ValidTo),\n            PurchaseUrl = offer.PurchaseUrl,\n            Conditions = offer.Conditions.ToHttp(),\n            SortOrder = offer.SortOrder,\n        };\n    }\n\n    private static ParkPriceValueDto ToHttp(this ParkPriceValue price)")

path = "API/AmusementPark.WebAPI/Controllers/ParkPricingController.cs"
replace(path,
"                mappingResult.Value,\n                PreserveHistoricalSnapshots: request.HistoricalSnapshots is null),",
"                mappingResult.Value,\n                PreserveHistoricalSnapshots: request.HistoricalSnapshots is null,\n                PreserveCreditOffers: request.CreditOffers is null),")

# -------------------------
# Graph import/export
# -------------------------
path = "API/AmusementPark.Application/Features/ParkGraphUpserts/Contracts/ParkGraphExportPricing.cs"
replace(path,
"    public List<ParkGraphExportParkingPriceOffer> ParkingOffers { get; init; } = new();\n\n    public List<ParkGraphExportPricingSnapshot> HistoricalSnapshots",
"    public List<ParkGraphExportParkingPriceOffer> ParkingOffers { get; init; } = new();\n\n    public List<ParkGraphExportCreditOffer> CreditOffers { get; init; } = new();\n\n    public List<ParkGraphExportPricingSnapshot> HistoricalSnapshots")
replace(path,
"    public List<ParkGraphExportParkingPriceOffer> ParkingOffers { get; init; } = new();\n}\n\npublic sealed class ParkGraphExportAdmissionPriceOffer",
"    public List<ParkGraphExportParkingPriceOffer> ParkingOffers { get; init; } = new();\n\n    public List<ParkGraphExportCreditOffer> CreditOffers { get; init; } = new();\n}\n\npublic sealed class ParkGraphExportAdmissionPriceOffer")
replace(path,
"public sealed class ParkGraphExportPriceValue\n{",
"public sealed class ParkGraphExportCreditOffer\n{\n    public string? Id { get; init; }\n\n    public string UnitCode { get; init; } = string.Empty;\n\n    public int Quantity { get; init; }\n\n    public List<LocalizedText> Labels { get; init; } = new();\n\n    public ParkGraphExportCreditOfferPrices Prices { get; init; } = new();\n\n    public string? ValidFrom { get; init; }\n\n    public string? ValidTo { get; init; }\n\n    public string? PurchaseUrl { get; init; }\n\n    public List<LocalizedText> Conditions { get; init; } = new();\n\n    public int SortOrder { get; init; }\n}\n\npublic sealed class ParkGraphExportCreditOfferPrices\n{\n    public decimal? OnlinePrice { get; init; }\n\n    public decimal? GatePrice { get; init; }\n}\n\npublic sealed class ParkGraphExportPriceValue\n{")

path = "API/AmusementPark.Application/Features/ParkGraphUpserts/Services/ParkGraphPricingExportMapper.cs"
credit_map = """            CreditOffers = pricing.CreditOffers
                .OrderBy(static offer => offer.SortOrder)
                .ThenBy(static offer => offer.UnitCode, StringComparer.Ordinal)
                .ThenBy(static offer => offer.Quantity)
                .Select(static offer => MapCreditOffer(offer))
                .ToList(),
"""
replace(path,
"            HistoricalSnapshots = pricing.HistoricalSnapshots",
credit_map + "            HistoricalSnapshots = pricing.HistoricalSnapshots")
replace(path,
"            ParkingOffers = snapshot.ParkingOffers\n                .OrderBy(static offer => offer.SortOrder)\n                .ThenBy(static offer => offer.Code, StringComparer.Ordinal)\n                .Select(static offer => new ParkGraphExportParkingPriceOffer\n                {\n                    Id = offer.Id,\n                    Code = offer.Code,\n                    Labels = CopyLocalizedTexts(offer.Labels),\n                    OnlinePrice = MapPriceValue(offer.OnlinePrice),\n                    GatePrice = MapPriceValue(offer.GatePrice),\n                    ValidFrom = FormatPricingDate(offer.ValidFrom),\n                    ValidTo = FormatPricingDate(offer.ValidTo),\n                    PurchaseUrl = offer.PurchaseUrl,\n                    Conditions = CopyLocalizedTexts(offer.Conditions),\n                    SortOrder = offer.SortOrder,\n                })\n                .ToList(),\n        };",
"            ParkingOffers = snapshot.ParkingOffers\n                .OrderBy(static offer => offer.SortOrder)\n                .ThenBy(static offer => offer.Code, StringComparer.Ordinal)\n                .Select(static offer => new ParkGraphExportParkingPriceOffer\n                {\n                    Id = offer.Id,\n                    Code = offer.Code,\n                    Labels = CopyLocalizedTexts(offer.Labels),\n                    OnlinePrice = MapPriceValue(offer.OnlinePrice),\n                    GatePrice = MapPriceValue(offer.GatePrice),\n                    ValidFrom = FormatPricingDate(offer.ValidFrom),\n                    ValidTo = FormatPricingDate(offer.ValidTo),\n                    PurchaseUrl = offer.PurchaseUrl,\n                    Conditions = CopyLocalizedTexts(offer.Conditions),\n                    SortOrder = offer.SortOrder,\n                })\n                .ToList(),\n            CreditOffers = snapshot.CreditOffers\n                .OrderBy(static offer => offer.SortOrder)\n                .ThenBy(static offer => offer.UnitCode, StringComparer.Ordinal)\n                .ThenBy(static offer => offer.Quantity)\n                .Select(static offer => MapCreditOffer(offer))\n                .ToList(),\n        };")
replace(path,
"    private static ParkGraphExportPriceValue? MapPriceValue(ParkPriceValue? price)",
"    private static ParkGraphExportCreditOffer MapCreditOffer(ParkCreditOffer offer)\n    {\n        return new ParkGraphExportCreditOffer\n        {\n            Id = offer.Id,\n            UnitCode = offer.UnitCode,\n            Quantity = offer.Quantity,\n            Labels = CopyLocalizedTexts(offer.Labels),\n            Prices = new ParkGraphExportCreditOfferPrices\n            {\n                OnlinePrice = offer.Prices.OnlinePrice,\n                GatePrice = offer.Prices.GatePrice,\n            },\n            ValidFrom = FormatPricingDate(offer.ValidFrom),\n            ValidTo = FormatPricingDate(offer.ValidTo),\n            PurchaseUrl = offer.PurchaseUrl,\n            Conditions = CopyLocalizedTexts(offer.Conditions),\n            SortOrder = offer.SortOrder,\n        };\n    }\n\n    private static ParkGraphExportPriceValue? MapPriceValue(ParkPriceValue? price)")

path = "API/AmusementPark.Application/Features/ParkGraphUpserts/Services/ParkGraphUpsertProcessor.Pricing.cs"
replace(path,
"        if (!HasProperty(patch, \"historicalSnapshots\") && existingPricing is not null)\n        {\n            pricing.HistoricalSnapshots = existingPricing.HistoricalSnapshots;\n        }",
"        if (existingPricing is not null)\n        {\n            if (!HasProperty(patch, \"historicalSnapshots\"))\n            {\n                pricing.HistoricalSnapshots = existingPricing.HistoricalSnapshots;\n            }\n\n            if (!HasProperty(patch, \"creditOffers\"))\n            {\n                pricing.CreditOffers = existingPricing.CreditOffers;\n            }\n        }")
replace(path,
"            || HasNonEmptyPricingArray(patch, \"parkingOffers\")\n            || HasNonEmptyPricingArray(patch, \"historicalSnapshots\");",
"            || HasNonEmptyPricingArray(patch, \"parkingOffers\")\n            || HasNonEmptyPricingArray(patch, \"creditOffers\")\n            || HasNonEmptyPricingArray(patch, \"historicalSnapshots\");")
replace(path,
"            ParkingOffers = ReadParkingOffers(patch, \"pricing\", errors),\n            HistoricalSnapshots = ReadHistoricalSnapshots(patch, errors),",
"            ParkingOffers = ReadParkingOffers(patch, \"pricing\", errors),\n            CreditOffers = ReadCreditOffers(patch, \"pricing\", errors),\n            HistoricalSnapshots = ReadHistoricalSnapshots(patch, errors),")
replace(path,
"                ParkingOffers = ReadParkingOffers(element, prefix, errors),\n            });",
"                ParkingOffers = ReadParkingOffers(element, prefix, errors),\n                CreditOffers = ReadCreditOffers(element, prefix, errors),\n            });")
replace(path,
"    private static List<AmusementPark.Core.Localization.LocalizedText> ReadPricingLocalizedTexts(",
"    private static List<ParkCreditOffer> ReadCreditOffers(JsonElement patch, string rootPrefix, List<string> errors)\n    {\n        JsonElement? array = GetArray(patch, \"creditOffers\");\n        if (array is null)\n        {\n            if (HasProperty(patch, \"creditOffers\"))\n            {\n                errors.Add($\"{rootPrefix}.creditOffers doit être un tableau.\");\n            }\n\n            return new List<ParkCreditOffer>();\n        }\n\n        List<ParkCreditOffer> offers = new();\n        int index = 0;\n        foreach (JsonElement element in array.Value.EnumerateArray())\n        {\n            string prefix = $\"{rootPrefix}.creditOffers[{index}]\";\n            if (element.ValueKind != JsonValueKind.Object)\n            {\n                errors.Add($\"{prefix} doit être un objet.\");\n                index += 1;\n                continue;\n            }\n\n            JsonElement? prices = GetObject(element, \"prices\");\n            if (prices is null && HasProperty(element, \"prices\"))\n            {\n                errors.Add($\"{prefix}.prices doit être un objet.\");\n            }\n\n            offers.Add(new ParkCreditOffer\n            {\n                Id = ReadString(element, \"id\"),\n                UnitCode = ReadString(element, \"unitCode\") ?? string.Empty,\n                Quantity = ReadInt(element, \"quantity\") ?? 0,\n                Labels = ReadPricingLocalizedTexts(element, \"labels\", prefix, errors),\n                Prices = new ParkCreditOfferPrices\n                {\n                    OnlinePrice = ReadOptionalPricingDecimal(prices, \"onlinePrice\", $\"{prefix}.prices\", errors),\n                    GatePrice = ReadOptionalPricingDecimal(prices, \"gatePrice\", $\"{prefix}.prices\", errors),\n                },\n                ValidFrom = ReadOptionalPricingDate(element, \"validFrom\", prefix, errors),\n                ValidTo = ReadOptionalPricingDate(element, \"validTo\", prefix, errors),\n                PurchaseUrl = ReadString(element, \"purchaseUrl\"),\n                Conditions = ReadPricingLocalizedTexts(element, \"conditions\", prefix, errors),\n                SortOrder = ReadInt(element, \"sortOrder\") ?? index + 1,\n            });\n            index += 1;\n        }\n\n        return offers;\n    }\n\n    private static decimal? ReadOptionalPricingDecimal(\n        JsonElement? element,\n        string propertyName,\n        string prefix,\n        List<string> errors)\n    {\n        if (element is null || !element.Value.TryGetProperty(propertyName, out JsonElement value) || value.ValueKind == JsonValueKind.Null)\n        {\n            return null;\n        }\n\n        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out decimal result))\n        {\n            return result;\n        }\n\n        errors.Add($\"{prefix}.{propertyName} doit être un nombre décimal.\");\n        return null;\n    }\n\n    private static List<AmusementPark.Core.Localization.LocalizedText> ReadPricingLocalizedTexts(")
replace(path,
"        AddChange(change, \"pricing.parkingOffers\", DescribePricing(existingPricing?.ParkingOffers), DescribePricing(normalizedPricing.ParkingOffers));\n        AddChange(change, \"pricing.historicalSnapshots\"",
"        AddChange(change, \"pricing.parkingOffers\", DescribePricing(existingPricing?.ParkingOffers), DescribePricing(normalizedPricing.ParkingOffers));\n        AddChange(change, \"pricing.creditOffers\", DescribePricing(existingPricing?.CreditOffers), DescribePricing(normalizedPricing.CreditOffers));\n        AddChange(change, \"pricing.historicalSnapshots\"")

# -------------------------
# Front model
# -------------------------
path = "FRONT/AmusementPark/src/app/models/parks/park-pricing.ts"
replace(path,
"export interface ParkPricing {",
"export interface ParkCreditOfferPrices {\n  onlinePrice?: number | null;\n  gatePrice?: number | null;\n}\n\nexport interface ParkCreditOffer {\n  id?: string | null;\n  unitCode: string;\n  quantity: number;\n  labels: LocalizedItem<string>[];\n  prices: ParkCreditOfferPrices;\n  validFrom?: string | null;\n  validTo?: string | null;\n  purchaseUrl?: string | null;\n  conditions: LocalizedItem<string>[];\n  sortOrder: number;\n}\n\nexport interface ParkPricing {")
replace(path,
"  parkingOffers: ParkParkingPriceOffer[];\n  historicalSnapshots?: ParkPricingSnapshot[];",
"  parkingOffers: ParkParkingPriceOffer[];\n  creditOffers?: ParkCreditOffer[];\n  historicalSnapshots?: ParkPricingSnapshot[];",
2)

# -------------------------
# Admin reusable credit editor
# -------------------------
admin_dir = "FRONT/AmusementPark/src/app/features/admin/parks/pages/admin-parks/admin-park-edit/tabs/admin-park-pricing-tab"
write(f"{admin_dir}/admin-park-pricing-credit-offer-editor.component.ts", """import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { ParkCreditOffer } from '@app/models/parks/park-pricing';
import { LocalizedItem } from '@app/models/shared/localized-item';
import { LocalizedTextInputComponent } from '@shared/components/localized-text-input/localized-text-input.component';
import { ButtonDirective } from '@shared/ui/primitives/button';

@Component({
  selector: 'app-admin-park-pricing-credit-offer-editor',
  templateUrl: './admin-park-pricing-credit-offer-editor.component.html',
  styleUrls: ['./admin-park-pricing-credit-offer-editor.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonDirective, FormsModule, LocalizedTextInputComponent, TranslateModule]
})
export class AdminParkPricingCreditOfferEditorComponent {
  @Input({ required: true }) offer!: ParkCreditOffer;
  @Input() disabled: boolean = false;

  @Output() readonly offerChange = new EventEmitter<ParkCreditOffer>();
  @Output() readonly remove = new EventEmitter<void>();

  protected updateUnitCode(value: string | null): void {
    this.emit({ unitCode: value ?? '' });
  }

  protected updateQuantity(value: string | number | null): void {
    this.emit({ quantity: Number(value) || 0 });
  }

  protected updateLabels(labels: LocalizedItem<string>[]): void {
    this.emit({ labels });
  }

  protected updatePrice(channel: 'onlinePrice' | 'gatePrice', value: string | number | null): void {
    const parsed: number | null = value === null || value === '' ? null : Number(value);
    this.emit({
      prices: {
        ...this.offer.prices,
        [channel]: Number.isFinite(parsed) ? parsed : null
      }
    });
  }

  protected updateDate(field: 'validFrom' | 'validTo', value: string | null): void {
    this.emit({ [field]: value || null });
  }

  protected updatePurchaseUrl(value: string | null): void {
    this.emit({ purchaseUrl: value });
  }

  protected updateConditions(conditions: LocalizedItem<string>[]): void {
    this.emit({ conditions });
  }

  protected updateSortOrder(value: string | number | null): void {
    this.emit({ sortOrder: Number(value) || 0 });
  }

  private emit(changes: Partial<ParkCreditOffer>): void {
    this.offerChange.emit({ ...this.offer, ...changes });
  }
}
""")
write(f"{admin_dir}/admin-park-pricing-credit-offer-editor.component.html", """<article class="credit-offer-editor">
  <header>
    <div>
      <span>{{ 'adminParkPricingCredits.offerKind' | translate }}</span>
      <strong>{{ (offer.labels[0]?.value || (offer.quantity + ' ' + offer.unitCode)) }}</strong>
    </div>
    <button appUiButton type="button" class="p-button-sm p-button-danger p-button-text" icon="pi pi-trash" [disabled]="disabled" [label]="'adminParkPricing.actions.removeOffer' | translate" (click)="remove.emit()"></button>
  </header>

  <div class="credit-offer-editor__fields">
    <label><span>{{ 'adminParkPricingCredits.fields.unitCode' | translate }}</span><input type="text" [disabled]="disabled" [ngModel]="offer.unitCode" [ngModelOptions]="{ standalone: true }" (ngModelChange)="updateUnitCode($event)" /></label>
    <label><span>{{ 'adminParkPricingCredits.fields.quantity' | translate }}</span><input type="number" min="1" step="1" [disabled]="disabled" [ngModel]="offer.quantity" [ngModelOptions]="{ standalone: true }" (ngModelChange)="updateQuantity($event)" /></label>
    <label><span>{{ 'adminParkPricingCredits.fields.onlinePrice' | translate }}</span><input type="number" min="0" step="0.01" [disabled]="disabled" [ngModel]="offer.prices.onlinePrice" [ngModelOptions]="{ standalone: true }" (ngModelChange)="updatePrice('onlinePrice', $event)" /></label>
    <label><span>{{ 'adminParkPricingCredits.fields.gatePrice' | translate }}</span><input type="number" min="0" step="0.01" [disabled]="disabled" [ngModel]="offer.prices.gatePrice" [ngModelOptions]="{ standalone: true }" (ngModelChange)="updatePrice('gatePrice', $event)" /></label>
    <label><span>{{ 'adminParkPricing.fields.validFrom' | translate }}</span><input type="date" [disabled]="disabled" [ngModel]="offer.validFrom" [ngModelOptions]="{ standalone: true }" (ngModelChange)="updateDate('validFrom', $event)" /></label>
    <label><span>{{ 'adminParkPricing.fields.validTo' | translate }}</span><input type="date" [disabled]="disabled" [ngModel]="offer.validTo" [ngModelOptions]="{ standalone: true }" (ngModelChange)="updateDate('validTo', $event)" /></label>
    <label><span>{{ 'adminParkPricing.fields.sortOrder' | translate }}</span><input type="number" min="1" [disabled]="disabled" [ngModel]="offer.sortOrder" [ngModelOptions]="{ standalone: true }" (ngModelChange)="updateSortOrder($event)" /></label>
    <label class="credit-offer-editor__wide"><span>{{ 'adminParkPricing.fields.offerPurchaseUrl' | translate }}</span><input type="url" [disabled]="disabled" [ngModel]="offer.purchaseUrl" [ngModelOptions]="{ standalone: true }" (ngModelChange)="updatePurchaseUrl($event)" /></label>
    <div class="credit-offer-editor__wide"><span>{{ 'adminParkPricing.fields.labels' | translate }}</span><app-localized-text-input [disabled]="disabled" [ngModel]="offer.labels" [ngModelOptions]="{ standalone: true }" [placeholderKey]="'adminParkPricing.placeholders.localizedValue'" [copyAllButtonLabel]="'adminParkPricing.actions.copyAllLanguages' | translate" (ngModelChange)="updateLabels($event)"></app-localized-text-input></div>
    <div class="credit-offer-editor__wide"><span>{{ 'adminParkPricing.fields.conditions' | translate }}</span><app-localized-text-input [disabled]="disabled" [ngModel]="offer.conditions" [ngModelOptions]="{ standalone: true }" [placeholderKey]="'adminParkPricing.placeholders.conditions'" [copyAllButtonLabel]="'adminParkPricing.actions.copyAllLanguages' | translate" (ngModelChange)="updateConditions($event)"></app-localized-text-input></div>
  </div>
</article>
""")
write(f"{admin_dir}/admin-park-pricing-credit-offer-editor.component.scss", """.credit-offer-editor {
  display: grid;
  gap: 1rem;
  padding: 1rem;
  border: 1px solid var(--surface-border, #dfe3e8);
  border-radius: .75rem;
}

.credit-offer-editor > header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}

.credit-offer-editor > header div {
  display: grid;
  gap: .2rem;
}

.credit-offer-editor__fields {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: .75rem;
}

.credit-offer-editor__fields label,
.credit-offer-editor__wide {
  display: grid;
  gap: .35rem;
}

.credit-offer-editor__wide {
  grid-column: 1 / -1;
}

input {
  width: 100%;
}
""")

# Admin main tab
path = f"{admin_dir}/admin-park-pricing-tab.component.ts"
replace(path,
"  ParkAnnualPassOffer,\n  ParkParkingPriceOffer,",
"  ParkAnnualPassOffer,\n  ParkCreditOffer,\n  ParkParkingPriceOffer,")
replace(path,
"import { AdminParkPricingSnapshotEditorComponent } from './admin-park-pricing-snapshot-editor.component';",
"import { AdminParkPricingSnapshotEditorComponent } from './admin-park-pricing-snapshot-editor.component';\nimport { AdminParkPricingCreditOfferEditorComponent } from './admin-park-pricing-credit-offer-editor.component';")
replace(path,
"    AdminParkPricingOfferEditorComponent,\n    AdminParkPricingSnapshotEditorComponent,",
"    AdminParkPricingOfferEditorComponent,\n    AdminParkPricingCreditOfferEditorComponent,\n    AdminParkPricingSnapshotEditorComponent,")
replace(path,
"  private offerClientKeySequence: number = 0;",
"  private readonly creditOfferClientKeys: string[] = [];\n  private offerClientKeySequence: number = 0;")
replace(path,
"  protected addAnnualPass(): void {",
"  protected addCreditOffer(): void {\n    const current: ParkPricing | null = this.pricing();\n    if (!current) {\n      return;\n    }\n\n    const offers: ParkCreditOffer[] = current.creditOffers ?? [];\n    const offer: ParkCreditOffer = {\n      unitCode: 'token',\n      quantity: 1,\n      labels: [],\n      prices: { onlinePrice: null, gatePrice: null },\n      validFrom: null,\n      validTo: null,\n      purchaseUrl: null,\n      conditions: [],\n      sortOrder: this.nextSortOrder(offers)\n    };\n    this.creditOfferClientKeys.push(this.nextOfferClientKey('creditOffers'));\n    this.pricing.set({ ...current, creditOffers: [...offers, offer] });\n  }\n\n  protected creditOfferTrackKey(index: number): string {\n    let key: string | undefined = this.creditOfferClientKeys[index];\n    if (!key) {\n      key = this.nextOfferClientKey('creditOffers');\n      this.creditOfferClientKeys[index] = key;\n    }\n\n    return key;\n  }\n\n  protected updateCreditOffer(index: number, offer: ParkCreditOffer): void {\n    const current: ParkPricing | null = this.pricing();\n    if (!current) {\n      return;\n    }\n\n    this.pricing.set({\n      ...current,\n      creditOffers: (current.creditOffers ?? []).map(\n        (item: ParkCreditOffer, itemIndex: number): ParkCreditOffer => itemIndex === index ? offer : item)\n    });\n  }\n\n  protected removeCreditOffer(index: number): void {\n    const current: ParkPricing | null = this.pricing();\n    if (!current) {\n      return;\n    }\n\n    this.creditOfferClientKeys.splice(index, 1);\n    this.pricing.set({\n      ...current,\n      creditOffers: (current.creditOffers ?? []).filter(\n        (_item: ParkCreditOffer, itemIndex: number): boolean => itemIndex !== index)\n    });\n  }\n\n  protected addAnnualPass(): void {")
replace(path,
"      parkingOffers: [],\n    };",
"      parkingOffers: [],\n      creditOffers: [],\n    };")
replace(path,
"        historicalSnapshots: loadedPricing.historicalSnapshots ?? []",
"        creditOffers: loadedPricing.creditOffers ?? [],\n        historicalSnapshots: loadedPricing.historicalSnapshots ?? []")
replace(path,
"      parkingOffers: [],\n      historicalSnapshots: [],",
"      parkingOffers: [],\n      creditOffers: [],\n      historicalSnapshots: [],")
replace(path,
"    this.offerClientKeys.parkingOffers = (pricing?.parkingOffers ?? [])\n      .map((): string => this.nextOfferClientKey('parkingOffers'));",
"    this.offerClientKeys.parkingOffers = (pricing?.parkingOffers ?? [])\n      .map((): string => this.nextOfferClientKey('parkingOffers'));\n    this.creditOfferClientKeys.splice(0, this.creditOfferClientKeys.length,\n      ...(pricing?.creditOffers ?? []).map((): string => this.nextOfferClientKey('creditOffers')));")
replace(path,
"  private nextOfferClientKey(collection: PricingCollection): string {",
"  private nextOfferClientKey(collection: PricingCollection | 'creditOffers'): string {")

path = f"{admin_dir}/admin-park-pricing-tab.component.html"
credit_section = """
    <section class="admin-pricing-editor__collection">
      <header>
        <div>
          <h4>{{ 'adminParkPricingCredits.title' | translate }}</h4>
          <p>{{ 'adminParkPricingCredits.hint' | translate }}</p>
        </div>
        <button appUiButton type="button" class="p-button-sm" icon="pi pi-plus" [disabled]="saving" [label]="'adminParkPricingCredits.add' | translate" (click)="addCreditOffer()"></button>
      </header>
      @for (offer of currentPricing.creditOffers ?? []; track creditOfferTrackKey($index); let index = $index) {
        <app-admin-park-pricing-credit-offer-editor [offer]="offer" [disabled]="saving" (offerChange)="updateCreditOffer(index, $event)" (remove)="removeCreditOffer(index)"></app-admin-park-pricing-credit-offer-editor>
      } @empty {
        <p class="admin-pricing-editor__empty">{{ 'adminParkPricingCredits.empty' | translate }}</p>
      }
    </section>

"""
replace(path,
"    <section class=\"admin-pricing-editor__collection\">\n      <header>\n        <div>\n          <h4>{{ 'adminParkPricing.sections.annualPasses' | translate }}</h4>",
credit_section + "    <section class=\"admin-pricing-editor__collection\">\n      <header>\n        <div>\n          <h4>{{ 'adminParkPricing.sections.annualPasses' | translate }}</h4>")

# Snapshot editor
path = f"{admin_dir}/admin-park-pricing-snapshot-editor.component.ts"
replace(path,
"  ParkAnnualPassOffer,\n  ParkParkingPriceOffer,",
"  ParkAnnualPassOffer,\n  ParkCreditOffer,\n  ParkParkingPriceOffer,")
replace(path,
"} from './admin-park-pricing-offer-editor.component';",
"} from './admin-park-pricing-offer-editor.component';\nimport { AdminParkPricingCreditOfferEditorComponent } from './admin-park-pricing-credit-offer-editor.component';")
replace(path,
"    AdminParkPricingOfferEditorComponent,\n    ButtonDirective,",
"    AdminParkPricingOfferEditorComponent,\n    AdminParkPricingCreditOfferEditorComponent,\n    ButtonDirective,")
replace(path,
"  protected addOffer(collection: SnapshotPricingCollection): void {",
"  protected addCreditOffer(): void {\n    const offers: ParkCreditOffer[] = this.snapshot.creditOffers ?? [];\n    const offer: ParkCreditOffer = {\n      unitCode: 'token',\n      quantity: 1,\n      labels: [],\n      prices: { onlinePrice: null, gatePrice: null },\n      validFrom: null,\n      validTo: null,\n      purchaseUrl: null,\n      conditions: [],\n      sortOrder: this.nextSortOrder(offers)\n    };\n    this.emit({ creditOffers: [...offers, offer] });\n  }\n\n  protected updateCreditOffer(index: number, offer: ParkCreditOffer): void {\n    this.emit({\n      creditOffers: (this.snapshot.creditOffers ?? []).map(\n        (item: ParkCreditOffer, itemIndex: number): ParkCreditOffer => itemIndex === index ? offer : item)\n    });\n  }\n\n  protected removeCreditOffer(index: number): void {\n    this.emit({\n      creditOffers: (this.snapshot.creditOffers ?? []).filter(\n        (_item: ParkCreditOffer, itemIndex: number): boolean => itemIndex !== index)\n    });\n  }\n\n  protected addOffer(collection: SnapshotPricingCollection): void {")

path = f"{admin_dir}/admin-park-pricing-snapshot-editor.component.html"
snapshot_credit = """
  <section class="pricing-snapshot-editor__collection">
    <header>
      <h6>{{ 'adminParkPricingCredits.title' | translate }}</h6>
      <button appUiButton type="button" class="p-button-sm" icon="pi pi-plus" [disabled]="disabled" [label]="'adminParkPricingCredits.add' | translate" (click)="addCreditOffer()"></button>
    </header>
    @for (offer of snapshot.creditOffers ?? []; track offer.id || $index; let index = $index) {
      <app-admin-park-pricing-credit-offer-editor [offer]="offer" [disabled]="disabled" (offerChange)="updateCreditOffer(index, $event)" (remove)="removeCreditOffer(index)"></app-admin-park-pricing-credit-offer-editor>
    }
  </section>

"""
replace(path,
"  <section class=\"pricing-snapshot-editor__collection\">\n    <header>\n      <h6>{{ 'adminParkPricing.sections.annualPasses' | translate }}</h6>",
snapshot_credit + "  <section class=\"pricing-snapshot-editor__collection\">\n    <header>\n      <h6>{{ 'adminParkPricing.sections.annualPasses' | translate }}</h6>")

# -------------------------
# Public presentation + page
# -------------------------
path = "FRONT/AmusementPark/src/app/features/public/parks/models/park-pricing.presentation.ts"
replace(path,
"  ParkAnnualPassOffer,\n  ParkParkingPriceOffer,",
"  ParkAnnualPassOffer,\n  ParkCreditOffer,\n  ParkParkingPriceOffer,")
replace(path,
"export type ParkPricingHistoryOfferKind = 'admission' | 'annualPass' | 'parking';",
"export type ParkPricingHistoryOfferKind = 'admission' | 'credit' | 'annualPass' | 'parking';")
replace(path,
"      parkingOffers: pricing.parkingOffers,\n      isCurrent: true",
"      parkingOffers: pricing.parkingOffers,\n      creditOffers: pricing.creditOffers ?? [],\n      isCurrent: true")
replace(path,
"    appendAdmissionHistory(snapshot, language, pointsBySeries);\n    appendAnnualPassHistory",
"    appendAdmissionHistory(snapshot, language, pointsBySeries);\n    appendCreditHistory(snapshot, language, pointsBySeries);\n    appendAnnualPassHistory")
replace(path,
"function appendAnnualPassHistory(",
"function appendCreditHistory(\n  snapshot: ParkPricingSnapshot,\n  language: string,\n  seriesByKey: Map<string, ParkPricingHistorySeries>\n): void {\n  for (const offer of snapshot.creditOffers ?? []) {\n    const code: string = `${offer.unitCode}:${offer.quantity}`;\n    appendCreditHistoryPoint(\n      snapshot,\n      code,\n      resolvePricingLocalizedText(offer.labels, language, `${offer.quantity} ${offer.unitCode}`),\n      offer,\n      seriesByKey);\n  }\n}\n\nfunction appendCreditHistoryPoint(\n  snapshot: ParkPricingSnapshot,\n  code: string,\n  label: string,\n  offer: ParkCreditOffer,\n  seriesByKey: Map<string, ParkPricingHistorySeries>\n): void {\n  const key: string = `credit:${code.trim().toLowerCase()}`;\n  const point: ParkPricingHistoryPoint = {\n    year: snapshot.year,\n    currencyCode: snapshot.currencyCode,\n    onlinePrice: fixedCreditPrice(offer.prices.onlinePrice),\n    gatePrice: fixedCreditPrice(offer.prices.gatePrice)\n  };\n  const existing: ParkPricingHistorySeries | undefined = seriesByKey.get(key);\n  if (existing) {\n    if (!existing.points.some((item: ParkPricingHistoryPoint): boolean => item.year === point.year)) {\n      existing.points.push(point);\n    }\n    return;\n  }\n\n  seriesByKey.set(key, { key, code, kind: 'credit', label, points: [point] });\n}\n\nfunction fixedCreditPrice(amount: number | null | undefined): ParkPriceValue | null {\n  return amount === null || amount === undefined ? null : { mode: 'Fixed', amount };\n}\n\nfunction appendAnnualPassHistory(")

path = "FRONT/AmusementPark/src/app/features/public/parks/pages/park-pricing-page.component.ts"
replace(path,
"      const offerCount: number = data.pricing\n        ? data.pricing.admissionOffers.length + data.pricing.annualPasses.length + data.pricing.parkingOffers.length",
"      const offerCount: number = data.pricing\n        ? data.pricing.admissionOffers.length + (data.pricing.creditOffers?.length ?? 0) + data.pricing.annualPasses.length + data.pricing.parkingOffers.length")
replace(path,
"        const offerCount: number = data.pricing\n          ? data.pricing.admissionOffers.length + data.pricing.annualPasses.length + data.pricing.parkingOffers.length",
"        const offerCount: number = data.pricing\n          ? data.pricing.admissionOffers.length + (data.pricing.creditOffers?.length ?? 0) + data.pricing.annualPasses.length + data.pricing.parkingOffers.length")
replace(path,
"  protected modeLabelKey(value: ParkPriceValue): string {",
"  protected formatCreditPrice(value: number | null | undefined, currencyCode: string): string | null {\n    if (value === null || value === undefined) {\n      return null;\n    }\n\n    return new Intl.NumberFormat(this.currentLanguage(), {\n      style: 'currency',\n      currency: currencyCode || 'EUR',\n      maximumFractionDigits: 2\n    }).format(value);\n  }\n\n  protected modeLabelKey(value: ParkPriceValue): string {")
replace(path,
"  protected historyKindLabelKey(series: ParkPricingHistorySeries): string {\n    return `parkPricing.history.kinds.${series.kind}`;\n  }",
"  protected historyKindLabelKey(series: ParkPricingHistorySeries): string {\n    return series.kind === 'credit'\n      ? 'parkPricingCredits.historyKind'\n      : `parkPricing.history.kinds.${series.kind}`;\n  }")

path = "FRONT/AmusementPark/src/app/features/public/parks/pages/park-pricing-page.component.html"
public_credit = """
          @if ((pricing.creditOffers?.length ?? 0) > 0) {
            <section class="park-pricing-section">
              <header class="park-pricing-section__header">
                <span class="park-pricing-section__icon"><i class="pi pi-wallet" aria-hidden="true"></i></span>
                <div>
                  <h2>{{ 'parkPricingCredits.title' | translate }}</h2>
                  <p>{{ 'parkPricingCredits.subtitle' | translate }}</p>
                </div>
              </header>

              <div class="park-pricing-grid">
                @for (offer of pricing.creditOffers ?? []; track offer.id || (offer.unitCode + ':' + offer.quantity)) {
                  <article class="park-price-card">
                    <header class="park-price-card__header">
                      <div>
                        <h3>{{ localizedText(offer.labels, offer.quantity + ' ' + offer.unitCode) }}</h3>
                        <span class="park-price-card__audience">{{ 'parkPricingCredits.quantity' | translate:{ quantity: offer.quantity, unit: offer.unitCode } }}</span>
                      </div>
                    </header>

                    <div class="park-price-card__prices">
                      @if (formatCreditPrice(offer.prices.onlinePrice, pricing.currencyCode); as price) {
                        <div class="park-price-channel"><span>{{ 'parkPricing.channels.online' | translate }}</span><strong>{{ price }}</strong></div>
                      }
                      @if (formatCreditPrice(offer.prices.gatePrice, pricing.currencyCode); as price) {
                        <div class="park-price-channel"><span>{{ 'parkPricing.channels.gate' | translate }}</span><strong>{{ price }}</strong></div>
                      }
                    </div>

                    @if (offer.validFrom || offer.validTo) {
                      <p class="park-price-card__validity">
                        <i class="pi pi-calendar" aria-hidden="true"></i>
                        @if (offer.validFrom && offer.validTo) {
                          {{ 'parkPricing.validity.range' | translate:{ from: formatDate(offer.validFrom), to: formatDate(offer.validTo) } }}
                        } @else if (offer.validFrom) {
                          {{ 'parkPricing.validity.from' | translate:{ date: formatDate(offer.validFrom) } }}
                        } @else if (offer.validTo) {
                          {{ 'parkPricing.validity.until' | translate:{ date: formatDate(offer.validTo) } }}
                        }
                      </p>
                    }

                    @if (localizedText(offer.conditions); as conditions) {
                      <div class="park-price-card__conditions"><strong>{{ 'parkPricing.fields.conditions' | translate }}</strong><p>{{ conditions }}</p></div>
                    }

                    @if (offer.purchaseUrl) {
                      <a class="park-price-card__buy" [href]="offer.purchaseUrl | safeExternalUrl" target="_blank" rel="noreferrer noopener"><span>{{ 'parkPricing.actions.buyOffer' | translate }}</span><i class="pi pi-external-link" aria-hidden="true"></i></a>
                    }
                  </article>
                }
              </div>
            </section>
          }

"""
replace(path,
"          @if (pricing.annualPasses.length > 0) {",
public_credit + "          @if (pricing.annualPasses.length > 0) {")

# -------------------------
# i18n source additions
# -------------------------
translations = {
    "fr": ("Jetons et crédits", "Configure les lots de jetons, crédits ou unités prépayées vendus par le parc.", "Ajouter un lot", "Aucun lot de jetons ou crédits.", "Lot de jetons / crédits", "Code de l’unité", "Quantité", "Prix en ligne", "Prix au guichet", "Jetons et crédits", "Lots prépayés utilisés pour accéder aux attractions", "{{ quantity }} × {{ unit }}", "Jetons / crédits"),
    "en": ("Tokens and credits", "Configure token, credit or prepaid-unit bundles sold by the park.", "Add bundle", "No token or credit bundle.", "Token / credit bundle", "Unit code", "Quantity", "Online price", "Gate price", "Tokens and credits", "Prepaid bundles used to access attractions", "{{ quantity }} × {{ unit }}", "Tokens / credits"),
    "es": ("Fichas y créditos", "Configura los lotes de fichas, créditos o unidades prepago vendidos por el parque.", "Añadir lote", "No hay lotes de fichas o créditos.", "Lote de fichas / créditos", "Código de unidad", "Cantidad", "Precio online", "Precio en taquilla", "Fichas y créditos", "Lotes prepago utilizados para acceder a las atracciones", "{{ quantity }} × {{ unit }}", "Fichas / créditos"),
    "de": ("Token und Guthaben", "Konfiguriert Token-, Guthaben- oder Prepaid-Pakete des Parks.", "Paket hinzufügen", "Keine Token- oder Guthabenpakete.", "Token-/Guthabenpaket", "Einheitencode", "Menge", "Onlinepreis", "Kassenpreis", "Token und Guthaben", "Prepaid-Pakete für die Nutzung von Attraktionen", "{{ quantity }} × {{ unit }}", "Token / Guthaben"),
    "it": ("Gettoni e crediti", "Configura i pacchetti di gettoni, crediti o unità prepagate venduti dal parco.", "Aggiungi pacchetto", "Nessun pacchetto di gettoni o crediti.", "Pacchetto gettoni / crediti", "Codice unità", "Quantità", "Prezzo online", "Prezzo in cassa", "Gettoni e crediti", "Pacchetti prepagati utilizzati per accedere alle attrazioni", "{{ quantity }} × {{ unit }}", "Gettoni / crediti"),
    "nl": ("Tokens en tegoeden", "Configureer bundels met tokens, tegoeden of prepaid-eenheden die het park verkoopt.", "Bundel toevoegen", "Geen token- of tegoedbundels.", "Token-/tegoedbundel", "Eenheidscode", "Aantal", "Onlineprijs", "Kassaprijs", "Tokens en tegoeden", "Prepaidbundels voor toegang tot attracties", "{{ quantity }} × {{ unit }}", "Tokens / tegoeden"),
    "pt": ("Fichas e créditos", "Configura os lotes de fichas, créditos ou unidades pré-pagas vendidos pelo parque.", "Adicionar lote", "Nenhum lote de fichas ou créditos.", "Lote de fichas / créditos", "Código da unidade", "Quantidade", "Preço online", "Preço na bilheteira", "Fichas e créditos", "Lotes pré-pagos utilizados para aceder às atrações", "{{ quantity }} × {{ unit }}", "Fichas / créditos"),
    "pl": ("Żetony i kredyty", "Konfiguruj pakiety żetonów, kredytów lub jednostek przedpłaconych sprzedawanych przez park.", "Dodaj pakiet", "Brak pakietów żetonów lub kredytów.", "Pakiet żetonów / kredytów", "Kod jednostki", "Ilość", "Cena online", "Cena w kasie", "Żetony i kredyty", "Pakiety przedpłacone używane do korzystania z atrakcji", "{{ quantity }} × {{ unit }}", "Żetony / kredyty"),
}
import json
for lang, values in translations.items():
    admin_title, admin_hint, admin_add, admin_empty, offer_kind, unit_code, quantity, online, gate, public_title, public_subtitle, public_quantity, history_kind = values
    write(f"FRONT/AmusementPark/src/assets/i18n/source/{lang}/admin/zz-park-pricing-credit-offers.json", json.dumps({
        "adminParkPricingCredits": {
            "title": admin_title,
            "hint": admin_hint,
            "add": admin_add,
            "empty": admin_empty,
            "offerKind": offer_kind,
            "fields": {"unitCode": unit_code, "quantity": quantity, "onlinePrice": online, "gatePrice": gate}
        }
    }, ensure_ascii=False, indent=2) + "\n")
    write(f"FRONT/AmusementPark/src/assets/i18n/source/{lang}/public/zz-park-pricing-credit-offers.json", json.dumps({
        "parkPricingCredits": {
            "title": public_title,
            "subtitle": public_subtitle,
            "quantity": public_quantity,
            "historyKind": history_kind
        }
    }, ensure_ascii=False, indent=2) + "\n")

# -------------------------
# Focused tests
# -------------------------
write("API/AmusementPark.Core.Tests/Domain/Parks/ParkPricingCreditOffersAvailabilityTests.cs", """using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Tests.Domain.Parks;

public sealed class ParkPricingCreditOffersAvailabilityTests
{
    [Fact]
    public void HasPricedOffersValidOn_IncludesCreditOffers()
    {
        ParkPricing pricing = new()
        {
            CreditOffers = new List<ParkCreditOffer>
            {
                new()
                {
                    UnitCode = "token",
                    Quantity = 10,
                    Labels = new List<LocalizedText> { new("fr", "10 jetons") },
                    Prices = new ParkCreditOfferPrices { GatePrice = 2500m },
                    ValidFrom = new DateOnly(2026, 1, 1),
                    ValidTo = new DateOnly(2026, 12, 31),
                },
            },
        };

        Assert.True(pricing.HasPricedOffersValidOn(new DateOnly(2026, 8, 17)));
        Assert.False(pricing.HasPricedOffersValidOn(new DateOnly(2027, 1, 1)));
        Assert.Single(pricing.FilterOffersValidOn(new DateOnly(2026, 8, 17)).CreditOffers);
    }
}
""")
write("API/AmusementPark.Application.Tests/Features/ParkPricing/Services/ParkPricingCreditOffersNormalizerTests.cs", """using AmusementPark.Application.Features.ParkPricing.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Tests.Features.ParkPricing.Services;

public sealed class ParkPricingCreditOffersNormalizerTests
{
    [Fact]
    public void Normalize_AcceptsCreditBundleAndNormalizesUnitCode()
    {
        ParkPricing pricing = CreatePricing();
        pricing.CreditOffers.Add(new ParkCreditOffer
        {
            UnitCode = " Token ",
            Quantity = 10,
            Labels = Localized("10 tokens"),
            Prices = new ParkCreditOfferPrices { GatePrice = 2500m },
            SortOrder = 1,
        });

        var result = ParkPricingNormalizer.Normalize(pricing);

        Assert.True(result.IsSuccess);
        Assert.Equal("token", result.Value!.CreditOffers.Single().UnitCode);
        Assert.Equal(2500m, result.Value.CreditOffers.Single().Prices.GatePrice);
    }

    [Fact]
    public void Normalize_RejectsDuplicateUnitAndQuantity()
    {
        ParkPricing pricing = CreatePricing();
        pricing.CreditOffers.Add(CreateOffer(10, 2500m));
        pricing.CreditOffers.Add(CreateOffer(10, 2400m));

        var result = ParkPricingNormalizer.Normalize(pricing);

        Assert.False(result.IsSuccess);
    }

    private static ParkPricing CreatePricing() => new()
    {
        ParkId = "park-1",
        CurrencyCode = "RSD",
        AdmissionOffers = new List<ParkAdmissionPriceOffer>
        {
            new()
            {
                Code = "entry",
                AudienceCategory = "general",
                Labels = Localized("Entry"),
                GatePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 1m },
                SortOrder = 1,
            },
        },
    };

    private static ParkCreditOffer CreateOffer(int quantity, decimal amount) => new()
    {
        UnitCode = "token",
        Quantity = quantity,
        Labels = Localized($"{quantity} tokens"),
        Prices = new ParkCreditOfferPrices { GatePrice = amount },
        SortOrder = 1,
    };

    private static List<LocalizedText> Localized(string value) => new()
    {
        new("fr", value), new("en", value), new("es", value), new("de", value),
        new("it", value), new("nl", value), new("pt", value), new("pl", value),
    };
}
""")
write("FRONT/AmusementPark/src/app/features/public/parks/models/park-pricing-credit-offers.presentation.spec.ts", """import { ParkPricing } from '@app/models/parks/park-pricing';
import { buildParkPricingHistorySeries } from './park-pricing.presentation';

describe('park pricing credit offers presentation', () => {
  it('builds a history series from unit code and quantity', () => {
    const pricing: ParkPricing = {
      parkId: 'park-1', currencyCode: 'RSD', notes: [], admissionOffers: [], annualPasses: [], parkingOffers: [],
      creditOffers: [{ unitCode: 'token', quantity: 10, labels: [{ languageCode: 'en', value: '10 tokens' }], prices: { gatePrice: 2500 }, conditions: [], sortOrder: 1 }],
      historicalSnapshots: [{ year: 2025, currencyCode: 'RSD', notes: [], admissionOffers: [], annualPasses: [], parkingOffers: [], creditOffers: [{ unitCode: 'token', quantity: 10, labels: [{ languageCode: 'en', value: '10 tokens' }], prices: { gatePrice: 2200 }, conditions: [], sortOrder: 1 }] }]
    };

    const [series] = buildParkPricingHistorySeries(pricing, 'en', 2026, 5);

    expect(series.kind).toBe('credit');
    expect(series.code).toBe('token:10');
    expect(series.points.map(point => point.gatePrice?.amount)).toEqual([2200, 2500]);
  });
});
""")

# -------------------------
# Documentation
# -------------------------
path = "docs/codex-guidelines/park-data-integration-steps/07-pricing.md"
replace(path,
"- `parkingOffers` : voiture, moto, camping-car ou autre offre de stationnement officiellement tarifée.",
"- `parkingOffers` : voiture, moto, camping-car ou autre offre de stationnement officiellement tarifée.\n- `creditOffers` : lots de jetons, crédits, points ou unités prépayées vendus par le parc pour accéder aux attractions.")
replace(path,
"Chaque offre comporte :",
"Pour `creditOffers`, conserver explicitement `unitCode` (code stable en minuscules), `quantity` (entier strictement positif), des `labels` localisés, et `prices.onlinePrice` et/ou `prices.gatePrice` sous forme de montants décimaux. Ne jamais confondre la quantité de jetons/crédits avec leur prix. Un même `unitCode` peut exister dans plusieurs quantités, mais le couple `unitCode` + `quantity` doit être unique dans une grille.\n\nChaque offre comporte :")
replace(path,
"- ses propres `admissionOffers`, `annualPasses` et `parkingOffers`, avec le même contrat que la grille actuelle.",
"- ses propres `admissionOffers`, `annualPasses`, `parkingOffers` et `creditOffers`, avec le même contrat que la grille actuelle.")
replace(path,
"    \"parkingOffers\": [],\n    \"historicalSnapshots\": [",
"    \"parkingOffers\": [],\n    \"creditOffers\": [\n      {\n        \"unitCode\": \"token\",\n        \"quantity\": 10,\n        \"labels\": [\n          { \"languageCode\": \"fr\", \"value\": \"10 jetons\" },\n          { \"languageCode\": \"en\", \"value\": \"10 tokens\" },\n          { \"languageCode\": \"es\", \"value\": \"10 fichas\" },\n          { \"languageCode\": \"de\", \"value\": \"10 Token\" },\n          { \"languageCode\": \"it\", \"value\": \"10 gettoni\" },\n          { \"languageCode\": \"nl\", \"value\": \"10 tokens\" },\n          { \"languageCode\": \"pt\", \"value\": \"10 fichas\" },\n          { \"languageCode\": \"pl\", \"value\": \"10 żetonów\" }\n        ],\n        \"prices\": { \"gatePrice\": 2500 },\n        \"conditions\": [],\n        \"sortOrder\": 1\n      }\n    ],\n    \"historicalSnapshots\": [")
replace(path,
"- une période de validité inversée ;",
"- une période de validité inversée ;\n- un `creditOffers.quantity` nul ou négatif, un `unitCode` vide, un couple `unitCode` + `quantity` dupliqué, un lot sans prix ou avec un montant négatif ;")

print("Credit offers implementation patched successfully.")
