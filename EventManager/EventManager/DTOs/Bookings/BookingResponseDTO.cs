using EventManager.Models;

namespace EventManager.DTOs.Bookings
{
    public class BookingResponseDTO
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public BookingStatus Status { get; set; }
    }
}
