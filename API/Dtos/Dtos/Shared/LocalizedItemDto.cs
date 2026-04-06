namespace Dtos.Shared
{
    public sealed class LocalizedItemDto<T>
    {
        public string LanguageCode { get; set; } = string.Empty;
        public T Value { get; set; } = default!;
    }
}
