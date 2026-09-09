using System.ComponentModel.DataAnnotations;

namespace EventManager.Application.DTOs.Users
{
    public class LoginRequestDTO
    {
        [Required(ErrorMessage = "Логин обязателен")]
        public string Login { get; set; } = null!;

        [Required(ErrorMessage = "Пароль обязателен")]
        public string Password { get; set; } = null!;
    }
}
