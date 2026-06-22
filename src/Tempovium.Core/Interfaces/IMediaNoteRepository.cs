using Tempovium.Core.Entities;

namespace Tempovium.Core.Interfaces.Repositories;

public interface IMediaNoteRepository
{
    Task<List<MediaNote>> GetNotesForMediaAsync(Guid mediaId);

    Task AddNoteAsync(MediaNote note);

    Task<MediaNote?> UpdateNoteAsync(Guid noteId, string content);

    Task DeleteNoteAsync(Guid noteId);

    Task SaveChangesAsync();
}
