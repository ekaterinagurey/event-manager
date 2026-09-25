using EventManager.Users.Domain.Entities;

namespace EventManager.Users.Application.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateToken(User user);
    }
}
