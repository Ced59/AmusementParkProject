using System.Text.Json.Serialization;

namespace Common.Users
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Role
    {
        USER,
        MODERATOR,
        ADMIN
    }
}