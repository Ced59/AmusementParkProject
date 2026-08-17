from pathlib import Path
p = Path(__file__).with_name('credit-offers-bootstrap.py')
t = p.read_text(encoding='utf-8')
def m(a,b):
    global t
    if a not in t: raise RuntimeError('fix anchor missing: '+repr(a[:120]))
    t=t.replace(a,b,1)
m('''replace(path,
"    public List<ParkParkingPriceOffer> ParkingOffers { get; set; } = new();\\n\\n    public List<ParkPricingSnapshot> HistoricalSnapshots { get; set; } = new();",
"    public List<ParkParkingPriceOffer> ParkingOffers { get; set; } = new();\\n\\n    public List<ParkCreditOffer> CreditOffers { get; set; } = new();\\n\\n    public List<ParkPricingSnapshot> HistoricalSnapshots { get; set; } = new();",
2)''','''replace(path,
"    public List<ParkParkingPriceOffer> ParkingOffers { get; set; } = new();\\n\\n    public List<ParkPricingSnapshot> HistoricalSnapshots { get; set; } = new();",
"    public List<ParkParkingPriceOffer> ParkingOffers { get; set; } = new();\\n\\n    public List<ParkCreditOffer> CreditOffers { get; set; } = new();\\n\\n    public List<ParkPricingSnapshot> HistoricalSnapshots { get; set; } = new();")
replace(path,
"    public List<ParkParkingPriceOffer> ParkingOffers { get; set; } = new();\\n\\n    public bool HasPricedOffers()",
"    public List<ParkParkingPriceOffer> ParkingOffers { get; set; } = new();\\n\\n    public List<ParkCreditOffer> CreditOffers { get; set; } = new();\\n\\n    public bool HasPricedOffers()")''')
m('''replace(path,
"            ParkingOffers = this.ParkingOffers.Where(offer => IsValidOn(offer.ValidFrom, offer.ValidTo, date)).ToList(),\\n            HistoricalSnapshots = this.HistoricalSnapshots.ToList(),",
"            ParkingOffers = this.ParkingOffers.Where(offer => IsValidOn(offer.ValidFrom, offer.ValidTo, date)).ToList(),\\n            CreditOffers = this.CreditOffers.Where(offer => IsValidOn(offer.ValidFrom, offer.ValidTo, date)).ToList(),\\n            HistoricalSnapshots = this.HistoricalSnapshots.ToList(),")''','''replace(path,
"            ParkingOffers = this.ParkingOffers\\n                .Where(offer => IsValidOn(offer.ValidFrom, offer.ValidTo, date))\\n                .ToList(),\\n            HistoricalSnapshots = this.HistoricalSnapshots",
"            ParkingOffers = this.ParkingOffers\\n                .Where(offer => IsValidOn(offer.ValidFrom, offer.ValidTo, date))\\n                .ToList(),\\n            CreditOffers = this.CreditOffers\\n                .Where(offer => IsValidOn(offer.ValidFrom, offer.ValidTo, date))\\n                .ToList(),\\n            HistoricalSnapshots = this.HistoricalSnapshots")''')
m('''replace(path,
"            || this.ParkingOffers.Any(offer => (offer.OnlinePrice is not null || offer.GatePrice is not null) && IsValidOn(offer.ValidFrom, offer.ValidTo, date));",
"            || this.ParkingOffers.Any(offer => (offer.OnlinePrice is not null || offer.GatePrice is not null) && IsValidOn(offer.ValidFrom, offer.ValidTo, date))\\n            || this.CreditOffers.Any(offer => (offer.Prices.OnlinePrice.HasValue || offer.Prices.GatePrice.HasValue) && IsValidOn(offer.ValidFrom, offer.ValidTo, date));")''','''replace(path,
"            || this.ParkingOffers.Any(offer => HasPrice(offer.OnlinePrice, offer.GatePrice)\\n                && IsValidOn(offer.ValidFrom, offer.ValidTo, date));",
"            || this.ParkingOffers.Any(offer => HasPrice(offer.OnlinePrice, offer.GatePrice)\\n                && IsValidOn(offer.ValidFrom, offer.ValidTo, date))\\n            || this.CreditOffers.Any(offer => (offer.Prices.OnlinePrice.HasValue || offer.Prices.GatePrice.HasValue)\\n                && IsValidOn(offer.ValidFrom, offer.ValidTo, date));")''')
m('''replace(path,
"  parkingOffers: ParkParkingPriceOffer[];\\n  historicalSnapshots?: ParkPricingSnapshot[];",
"  parkingOffers: ParkParkingPriceOffer[];\\n  creditOffers?: ParkCreditOffer[];\\n  historicalSnapshots?: ParkPricingSnapshot[];",
2)''','''replace(path,
"  parkingOffers: ParkParkingPriceOffer[];\\n  historicalSnapshots?: ParkPricingSnapshot[];",
"  parkingOffers: ParkParkingPriceOffer[];\\n  creditOffers?: ParkCreditOffer[];\\n  historicalSnapshots?: ParkPricingSnapshot[];")
replace(path,
"  parkingOffers: ParkParkingPriceOffer[];\\n}",
"  parkingOffers: ParkParkingPriceOffer[];\\n  creditOffers?: ParkCreditOffer[];\\n}")''')
m('''replace(path,
"      parkingOffers: [],\\n    };",
"      parkingOffers: [],\\n      creditOffers: [],\\n    };")''','''replace(path,
"      parkingOffers: []\\n    };",
"      parkingOffers: [],\\n      creditOffers: []\\n    };")''')
m('''write("API/AmusementPark.Core.Tests/Domain/Parks/ParkPricingCreditOffersAvailabilityTests.cs", """using AmusementPark.Core.Domain.Parks;''','''write("API/AmusementPark.Core.Tests/Domain/Parks/ParkPricingCreditOffersAvailabilityTests.cs", """using Xunit;\n\nusing AmusementPark.Core.Domain.Parks;''')
m('''write("API/AmusementPark.Application.Tests/Features/ParkPricing/Services/ParkPricingCreditOffersNormalizerTests.cs", """using AmusementPark.Application.Features.ParkPricing.Services;''','''write("API/AmusementPark.Application.Tests/Features/ParkPricing/Services/ParkPricingCreditOffersNormalizerTests.cs", """using Xunit;\n\nusing ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;\n\nusing AmusementPark.Application.Features.ParkPricing.Services;''')
m('''        ParkPricing pricing = CreatePricing();''','''        ParkPricingEntity pricing = CreatePricing();''')
m('''        ParkPricing pricing = CreatePricing();''','''        ParkPricingEntity pricing = CreatePricing();''')
m('''    private static ParkPricing CreatePricing() => new()''','''    private static ParkPricingEntity CreatePricing() => new()''')
m('''[channel]: Number.isFinite(parsed) ? parsed : null''','''[channel]: parsed !== null && Number.isFinite(parsed) ? parsed : null''')
p.write_text(t,encoding='utf-8')
print('bootstrap adjusted')
