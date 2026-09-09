using EventManager.Application.DTOs.Bookings;
using EventManager.Domain.Models;

namespace EventManager.Application.Mappers
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
