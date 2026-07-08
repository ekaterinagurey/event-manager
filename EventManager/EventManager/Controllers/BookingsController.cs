using EventManager.DTOs.Bookings;
using EventManager.Interfaces;
using EventManager.Mappers;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<BookingResponseDTO>> GetById(Guid id)
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);
            return Ok(booking.ToResponse());
        }
    }
}
