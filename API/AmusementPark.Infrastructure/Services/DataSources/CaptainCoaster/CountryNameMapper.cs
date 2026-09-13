using AmusementPark.Application.Features.DataSources.Contracts;
using AmusementPark.Application.Features.DataSources.Results;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Services.DataSources.Acquisition;
using AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using System.Globalization;
using System.Threading.Channels;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AmusementPark.Application.Features.Search;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster;

internal static class CountryNameMapper
{
    private static readonly Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["France"]="FR",["Allemagne"]="DE",["Belgique"]="BE",["Pays-Bas"]="NL",["Espagne"]="ES",
        ["Italie"]="IT",["Royaume-Uni"]="GB",["Grande-Bretagne"]="GB",["Suisse"]="CH",["Autriche"]="AT",
        ["Portugal"]="PT",["Suède"]="SE",["Danemark"]="DK",["Finlande"]="FI",["Norvège"]="NO",
        ["Pologne"]="PL",["République tchèque"]="CZ",["Tchéquie"]="CZ",["Hongrie"]="HU",["Roumanie"]="RO",
        ["Slovaquie"]="SK",["Slovénie"]="SI",["Croatie"]="HR",["Grèce"]="GR",["Turquie"]="TR",
        ["Russie"]="RU",["Ukraine"]="UA",["États-Unis"]="US",["Etats-Unis"]="US",["USA"]="US",
        ["Canada"]="CA",["Mexique"]="MX",["Brésil"]="BR",["Argentine"]="AR",["Chili"]="CL",
        ["Colombie"]="CO",["Pérou"]="PE",["Japon"]="JP",["Chine"]="CN",["Corée du Sud"]="KR",
        ["Corée"]="KR",["Australie"]="AU",["Nouvelle-Zélande"]="NZ",["Inde"]="IN",["Thaïlande"]="TH",
        ["Malaisie"]="MY",["Singapour"]="SG",["Indonésie"]="ID",["Philippines"]="PH",["Vietnam"]="VN",
        ["Bahreïn"]="BH",["Bahrain"]="BH",["Émirats arabes unis"]="AE",["Arabie saoudite"]="SA",
        ["Qatar"]="QA",["Koweït"]="KW",["Afrique du Sud"]="ZA",
        ["Germany"]="DE",["Belgium"]="BE",["Netherlands"]="NL",["Spain"]="ES",["Italy"]="IT",
        ["United Kingdom"]="GB",["Switzerland"]="CH",["Austria"]="AT",["Sweden"]="SE",["Denmark"]="DK",
        ["Finland"]="FI",["Norway"]="NO",["Poland"]="PL",["Czech Republic"]="CZ",["Hungary"]="HU",
        ["Romania"]="RO",["Slovakia"]="SK",["Slovenia"]="SI",["Croatia"]="HR",["Greece"]="GR",
        ["Turkey"]="TR",["Russia"]="RU",["United States"]="US",["Mexico"]="MX",["Brazil"]="BR",
        ["Argentina"]="AR",["Chile"]="CL",["Japan"]="JP",["China"]="CN",["South Korea"]="KR",
        ["Australia"]="AU",["New Zealand"]="NZ",["India"]="IN",["Thailand"]="TH",["Malaysia"]="MY",
        ["Indonesia"]="ID",["South Africa"]="ZA",
        ["country.belgium"]="BE",["country.france"]="FR",["country.germany"]="DE",["country.uk"]="GB",
        ["country.usa"]="US",["country.spain"]="ES",["country.italy"]="IT",["country.netherlands"]="NL",
        ["country.poland"]="PL",["country.sweden"]="SE",["country.denmark"]="DK",["country.finland"]="FI",
        ["country.switzerland"]="CH",["country.austria"]="AT",["country.portugal"]="PT",
        ["country.japan"]="JP",["country.canada"]="CA",["country.brazil"]="BR"
    };

    public static string? ToCountryCode(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue)) { return null; }
        string trimmed = rawValue.Trim();
        if (Values.TryGetValue(trimmed, out string? code)) { return code; }
        if (trimmed.Length == 2) { return trimmed.ToUpperInvariant(); }
        return null;
    }

}
