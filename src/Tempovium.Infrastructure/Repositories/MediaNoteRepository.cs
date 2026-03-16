using Microsoft.EntityFrameworkCore;
using Tempovium.Core.Entities;
using Tempovium.Core.Interfaces.Repositories;
using Tempovium.Infrastructure.Persistence;

namespace Tempovium.Infrastructure.Repositories;

public class MediaNoteRepository : IMediaNoteRepository
{
    private readonly TempoviumDbContext _context;

    public MediaNoteRepository(TempoviumDbContext context)
    {
        _context = context;
    }

    public async Task<List<MediaNote>> GetNotesForMediaAsync(Guid mediaId)
    {
        return await _context.MediaNotes
            .Where(n => n.MediaItemId == mediaId)
            .OrderBy(n => n.TimestampSeconds)
            .ToListAsync();
    }

    public async Task AddNoteAsync(MediaNote note)
    {
        await _context.MediaNotes.AddAsync(note);
    }

    public async Task DeleteNoteAsync(Guid noteId)
    {
        var note = await _context.MediaNotes.FindAsync(noteId);

        if (note != null)
        {
            _context.MediaNotes.Remove(note);
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}