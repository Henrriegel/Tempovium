using Microsoft.EntityFrameworkCore;
using Tempovium.Core.Entities;
using Tempovium.Core.Interfaces;
using Tempovium.Infrastructure.Persistence;

namespace Tempovium.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly TempoviumDbContext _db;

    public UserRepository(TempoviumDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _db.Users
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _db.Users
            .FirstOrDefaultAsync(x => x.Username == username);
    }

    public async Task CreateAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }
}