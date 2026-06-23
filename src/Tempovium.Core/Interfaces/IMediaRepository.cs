using Tempovium.Core.Entities;

namespace Tempovium.Core.Interfaces;

public interface IMediaRepository
{
    Task<MediaItem?> GetByIdAsync(Guid id);
    
    Task<List<MediaItem>> GetByUserAsync(Guid user);
    
    Task<MediaItem?> GetByHashAsync(Guid userId, string hash);

    Task<MediaItem?> GetByOriginalSourcePathAsync(Guid userId, string originalSourcePath);
    
    Task CreateAsync(MediaItem media);
    
    Task UpdateAsync(MediaItem media);
    
    Task DeleteAsync(Guid id);
}
