using System.Text.Json.Serialization;

namespace Entities.Model.Users.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Role
    {
        USER,
        MODERATOR,
        ADMIN
    }
}
