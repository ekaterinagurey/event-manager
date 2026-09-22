using EventManager.Bookings.Application.DTOs;
using EventManager.Bookings.Domain.Entities;

namespace EventManager.Bookings.Application.Mappers
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
