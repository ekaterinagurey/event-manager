using EventManager.Application.Interfaces.Authentication;
using EventManager.Application.Interfaces.Repositories;
using EventManager.Application.Services.Interfaces;
using EventManager.Domain.Enums;
using EventManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Application.Services
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
                          UserRole role,
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
                                   role);

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
                throw new InvalidOperationException("Invalid login or password.");

            return _jwtTokenService.GenerateToken(user);
        }
    }
}
