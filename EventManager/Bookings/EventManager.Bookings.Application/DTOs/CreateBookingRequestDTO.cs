using System.ComponentModel.DataAnnotations;

namespace EventManager.Bookings.Application.DTOs
{
    public class CreateBookingRequestDTO
    {
        [Required(ErrorMessage = "Идентификатор события обязателен.")]
        public Guid EventId { get; set; }
    }
}
