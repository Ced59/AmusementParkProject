using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PassportExportFormatDto
{
    Json,
    Csv,
}
