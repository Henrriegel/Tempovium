using Tempovium.Core.Entities;

namespace Tempovium.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    
    Task<User?> GetByUsernameAsync(string username);
    
    Task CreateAsync(User user);
    
    Task UpdateAsync(User user);
}