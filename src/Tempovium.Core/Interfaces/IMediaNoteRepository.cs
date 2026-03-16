using Tempovium.Core.Entities;

namespace Tempovium.Core.Interfaces.Repositories;

public interface IMediaNoteRepository
{
    Task<List<MediaNote>> GetNotesForMediaAsync(Guid mediaId);

    Task AddNoteAsync(MediaNote note);

    Task DeleteNoteAsync(Guid noteId);

    Task SaveChangesAsync();
}