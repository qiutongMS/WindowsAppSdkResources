using System.Collections.Generic;
using System.Threading.Tasks;
using BlankApp.Models;

namespace BlankApp.Data;

/// <summary>
/// Repository interface for user data access operations.
/// Following Repository Pattern and Dependency Inversion Principle.
/// </summary>
public interface IUserRepositorySample
{
    Task<UserSample?> GetByIdAsync(int id);
    Task<IEnumerable<UserSample>> GetAllAsync();
    Task<int> CreateAsync(UserSample user);
    Task UpdateAsync(UserSample user);
    Task DeleteAsync(int id);
}
