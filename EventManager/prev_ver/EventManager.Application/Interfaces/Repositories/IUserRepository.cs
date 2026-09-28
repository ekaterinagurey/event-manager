using EventManager.Domain.Models;

namespace EventManager.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken);
        Task CreateAsync(User user, CancellationToken cancellationToken);
    }
}
