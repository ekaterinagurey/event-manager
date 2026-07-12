using EventManager.DTOs.Bookings;
using EventManager.DTOs.Events;
using EventManager.Models;

namespace EventManager.Mappers
{
    public static class BookingMapper
    {
        public static BookingResponseDTO ToResponse(this Booking booking)
        {
            return new BookingResponseDTO
            {
                Id = booking.Id,
                EventId = booking.EventId,
                Status = booking.Status,
            };
        }
    }
}
