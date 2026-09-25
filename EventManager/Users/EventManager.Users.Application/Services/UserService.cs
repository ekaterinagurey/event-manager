using EventManager.Users.Domain.Enums;
using EventManager.Users.Domain.Exceptions;
using EventManager.Users.Domain.Entities;
using EventManager.Users.Application.Interfaces;
using EventManager.Users.Domain.Repositories;

namespace EventManager.Users.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;

        public UserService(IUserRepository userRepository,
                           IPasswordHasher passwordHasher,
                           IJwtTokenService jwtTokenService)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
        }

        public async Task RegisterAsync(string login,
                          string password,
                          CancellationToken cancellationToken)
        {
            var normalizedLogin = login.Trim().ToLowerInvariant();

            var existingUser = await _userRepository.GetByLoginAsync(normalizedLogin, cancellationToken);

            if (existingUser != null)
            {
                throw new InvalidOperationException("User with this login is already exist.");
            }

            var passwordHash = _passwordHasher.HashPassword(password);

            var user = User.Create(normalizedLogin,
                                   passwordHash,
                                   UserRole.User);

            await _userRepository.CreateAsync(user, cancellationToken);
        }

        public async Task<string> LoginAsync(string login,
                                             string password,
                                             CancellationToken cancellationToken)
        {
            var normalizedLogin = login.Trim().ToLowerInvariant();

            var user = await _userRepository.GetByLoginAsync(normalizedLogin, cancellationToken);

            if (user == null ||
               !_passwordHasher.VerifyPassword(password, user.PasswordHash))
                throw new UnauthorizedException();

            return _jwtTokenService.GenerateToken(user);
        }
    }
}
