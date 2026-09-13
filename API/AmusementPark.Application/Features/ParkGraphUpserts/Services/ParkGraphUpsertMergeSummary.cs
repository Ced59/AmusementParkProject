using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;
using AmusementPark.Core.Domain.History;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;
using AmusementPark.Application.Features.Parks.Services;
using System.Text;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.ParkPricing.Services;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

internal sealed class ParkGraphUpsertMergeSummary
{
    public Dictionary<string, string> ManufacturerIdRemaps { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public Dictionary<string, string> ParkIdRemaps { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public HashSet<string> ChangedParkIds { get; } = new HashSet<string>(StringComparer.Ordinal);

    public HashSet<string> ChangedParkItemIds { get; } = new HashSet<string>(StringComparer.Ordinal);

    public List<PublicSeoParkSnapshot> PreviousParks { get; } = new List<PublicSeoParkSnapshot>();

    public List<PublicSeoParkSnapshot> CurrentParks { get; } = new List<PublicSeoParkSnapshot>();

    public List<PublicSeoParkItemSnapshot> PreviousParkItems { get; } = new List<PublicSeoParkItemSnapshot>();

    public List<PublicSeoParkItemSnapshot> CurrentParkItems { get; } = new List<PublicSeoParkItemSnapshot>();
}
