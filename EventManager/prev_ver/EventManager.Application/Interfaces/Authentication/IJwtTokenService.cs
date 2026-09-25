using EventManager.Domain.Models;

namespace EventManager.Application.Interfaces.Authentication
{
    public interface IJwtTokenService
    {
        string GenerateToken(User user);
    }
}
