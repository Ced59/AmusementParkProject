using MongoDB.Bson.Serialization.Attributes;

namespace Common.General.Localization
{
    public class LocalizedItem<T>
    {
        [BsonElement("languageCode")]
        public string LanguageCode { get; set; } = default!;

        [BsonElement("value")]
        public T Value { get; set; } = default!;
    }
}
