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

internal static class PartialDateParser
{
    private static readonly Dictionary<string, int> MonthNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["janvier"]=1,["january"]=1,["jan"]=1,
        ["février"]=2,["february"]=2,["feb"]=2,
        ["mars"]=3,["march"]=3,["mar"]=3,
        ["avril"]=4,["april"]=4,["apr"]=4,
        ["mai"]=5,["may"]=5,
        ["juin"]=6,["june"]=6,["jun"]=6,
        ["juillet"]=7,["july"]=7,["jul"]=7,
        ["août"]=8,["aout"]=8,["august"]=8,["aug"]=8,
        ["septembre"]=9,["september"]=9,["sep"]=9,
        ["octobre"]=10,["october"]=10,["oct"]=10,
        ["novembre"]=11,["november"]=11,["nov"]=11,
        ["décembre"]=12,["decembre"]=12,["december"]=12,["dec"]=12
    };

    public static DateTime? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) { return null; }
        string trimmed = raw.Trim();

        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime full))
        {
            return full;
        }
        if (Regex.IsMatch(trimmed, @"^\d{4}-\d{2}$"))
        {
            string[] parts = trimmed.Split('-');
            if (int.TryParse(parts[0], out int y) && int.TryParse(parts[1], out int m) && m >= 1 && m <= 12)
            {
                return new DateTime(y, m, 1, 0, 0, 0, DateTimeKind.Utc);
            }
        }
        if (Regex.IsMatch(trimmed, @"^\d{4}$") && int.TryParse(trimmed, out int yearOnly) && yearOnly >= 1800 && yearOnly <= 2100)
        {
            return new DateTime(yearOnly, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }
        Match yearMatch = Regex.Match(trimmed, @"\b(\d{4})\b");
        if (yearMatch.Success && int.TryParse(yearMatch.Value, out int yearFromText) && yearFromText >= 1800 && yearFromText <= 2100)
        {
            foreach (KeyValuePair<string, int> entry in MonthNames)
            {
                if (trimmed.IndexOf(entry.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return new DateTime(yearFromText, entry.Value, 1, 0, 0, 0, DateTimeKind.Utc);
                }
            }
            return new DateTime(yearFromText, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }
        return null;
    }
}
