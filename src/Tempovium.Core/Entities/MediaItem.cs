using Tempovium.Core.Enums;

namespace Tempovium.Core.Entities;

public class MediaItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = String.Empty;
    public string FilePath { get; set; } = String.Empty;
    public MediaType MediaType { get; set; }
    public double DurationSeconds { get; set; }
    public string FileHash { get; set; } = String.Empty;
    public DateTime CreatedAt { get; set; }
}