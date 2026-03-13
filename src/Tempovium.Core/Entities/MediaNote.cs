namespace Tempovium.Core.Entities;

public class MediaNote
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid MediaItemId { get; set; }
    public double TimestampSeconds { get; set; }
    public string Content { get; set; } = String.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}