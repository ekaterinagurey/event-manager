namespace EventManager.Users.Application.Interfaces
{
    public interface IUserService
    {
        Task RegisterAsync(string login,
                           string password,
                           CancellationToken cancellationToken);

        Task<string> LoginAsync(string login,
                                string password,
                                CancellationToken cancellationToken);
    }
}
