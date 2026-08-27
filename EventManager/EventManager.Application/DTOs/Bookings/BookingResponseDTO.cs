using EventManager.Domain.Models;

namespace EventManager.Application.DTOs.Bookings
{
    public class BookingResponseDTO
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public BookingStatus Status { get; set; }
    }
}
