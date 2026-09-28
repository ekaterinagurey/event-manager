using EventManager.Bookings.Domain.Enums;

namespace EventManager.Bookings.Application.DTOs
{
    public class BookingResponseDTO
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public BookingStatus Status { get; set; }
    }
}
