using EventManager.Users.Domain.Entities;

namespace EventManager.Users.Domain.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken);
        Task CreateAsync(User user, CancellationToken cancellationToken);
    }
}
