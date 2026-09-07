using EventManager.Application.DTOs.Users;
using EventManager.Application.Services.Interfaces;
using EventManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System;

namespace EventManager.Controllers
{
    [ApiController]
    [Route("auth")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequestDTO request, CancellationToken cancellationToken)
        {
            if (string.Equals(request.Role.ToString(), nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    status = 400,
                    detail = "Регистрация с правами администратора запрещена"
                });
            }

            await _userService.RegisterAsync(request.Login,
                                             request.Password,
                                             UserRole.User,
                                             cancellationToken);
            return NoContent();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request, CancellationToken cancellationToken)
        {
            var token = await _userService.LoginAsync(request.Login,
                                                      request.Password,
                                                      cancellationToken);

            return Ok(new { Token = token });
        }
    }
}
