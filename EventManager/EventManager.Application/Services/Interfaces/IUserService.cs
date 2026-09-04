using EventManager.Domain.Enums;

namespace EventManager.Application.Services.Interfaces
{
    public interface IUserService
    {
        Task RegisterAsync(string login,
                           string password,
                           UserRole role,
                           CancellationToken cancellationToken);

        Task<string> LoginAsync(string login,
                                string password,
                                CancellationToken cancellationToken);
    }
}
