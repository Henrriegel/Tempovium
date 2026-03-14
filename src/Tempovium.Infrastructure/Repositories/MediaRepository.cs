using Microsoft.EntityFrameworkCore;
using Tempovium.Core.Entities;
using Tempovium.Core.Interfaces;
using Tempovium.Infrastructure.Persistence;

namespace Tempovium.Infrastructure.Repositories;

public class MediaRepository : IMediaRepository
{
    private readonly TempoviumDbContext _db;

    public MediaRepository(TempoviumDbContext db)
    {
        _db = db;
    }

    public async Task<MediaItem?> GetByIdAsync(Guid id)
    {
        return await _db.MediaItems
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<MediaItem>> GetByUserAsync(Guid userId)
    {
        return await _db.MediaItems
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<MediaItem?> GetByHashAsync(string hash)
    {
        return await _db.MediaItems
            .FirstOrDefaultAsync(x => x.FileHash == hash);
    }

    public async Task CreateAsync(MediaItem media)
    {
        _db.MediaItems.Add(media);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(MediaItem media)
    {
        _db.MediaItems.Update(media);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var media = await _db.MediaItems.FirstOrDefaultAsync(x => x.Id == id);

        if (media is null)
        {
            return;
        }

        _db.MediaItems.Remove(media);
        await _db.SaveChangesAsync();
    }
}