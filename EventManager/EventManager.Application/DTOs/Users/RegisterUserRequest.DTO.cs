using EventManager.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace EventManager.Application.DTOs.Users
{
    public class RegisterUserRequestDTO
    {
        [Required(ErrorMessage = "Логин обязателен")]
        public string Login { get; set; } = null!;

        [Required(ErrorMessage = "Пароль обязателен")]
        public string Password{ get; set; } = null!;
        public UserRole Role { get; set; } = UserRole.User;
    }
}
