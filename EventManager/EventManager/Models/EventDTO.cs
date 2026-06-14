using System.ComponentModel.DataAnnotations;

namespace EventManager.Models
{
    public class EventDTO : IValidatableObject
    {
        [Required(ErrorMessage = "Идентификатор события обязателен для заполнения.")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Заголовок события обязателен для заполнения.")]
        public string Title { get; set; }
        public string Description { get; set; }

        [Required(ErrorMessage = "Поле 'StartAt' обязательно для заполнения.")]
        public DateTime StartAt { get; set; }

        [Required(ErrorMessage = "Поле 'EndAt' обязательно для заполнения.")]
        public DateTime EndAt { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndAt <= StartAt)
            {
                yield return new ValidationResult("EndAt не может быть позже StartAt.", new[] { nameof(EndAt) });
            }
        }
    }
}
