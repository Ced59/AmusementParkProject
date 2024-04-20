namespace Entities.Model
{
    public abstract class ModelBase
    {
        public string? Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Language { get; set; }
    }
}
