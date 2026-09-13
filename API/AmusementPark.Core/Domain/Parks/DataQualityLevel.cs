using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;
using System.Net;
using System.Text.RegularExpressions;

namespace AmusementPark.Core.Domain.Parks;

public enum DataQualityLevel
{
    Critical = 0,
    Weak = 1,
    Partial = 2,
    Publishable = 3,
    Good = 4,
    Excellent = 5,
}
