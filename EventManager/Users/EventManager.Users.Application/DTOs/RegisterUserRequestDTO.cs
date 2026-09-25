using System.ComponentModel.DataAnnotations;

namespace EventManager.Users.Application.DTOs
{
    public class RegisterUserRequestDTO
    {
        [Required(ErrorMessage = "Логин обязателен")]
        public string Login { get; set; } = null!;

        [Required(ErrorMessage = "Пароль обязателен")]
        public string Password { get; set; } = null!;
    }
}
