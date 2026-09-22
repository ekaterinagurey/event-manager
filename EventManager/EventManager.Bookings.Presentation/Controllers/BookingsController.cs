using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EventManager.Bookings.Application.Interfaces;
using EventManager.Bookings.Domain.Entities;
using EventManager.Bookings.Domain.Exceptions;
using EventManager.Bookings.Application.DTOs;

namespace EventManager.Bookings.Presentation.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly ICurrentUserService _currentUserService;

        public BookingsController(IBookingService bookingService,
                                  ICurrentUserService currentUserService)
        {
            _bookingService = bookingService;
            _currentUserService = currentUserService;
        }

        [Authorize]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Booking>> GetById(Guid id)
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);

            var currentUserId = _currentUserService.UserId
                ?? throw new UnauthorizedException();

            if (booking.UserId != currentUserId && !_currentUserService.IsAdmin)
            {
                throw new AccessDeniedException("У вас нет прав на отмену этой брони.");
            }

            return Ok(booking);
        }

        [HttpPost]
        [ProducesResponseType(typeof(BookingResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<BookingResponseDTO>> Create([FromBody] CreateBookingRequestDTO dto,
                                                                   CancellationToken ct)
        {
            var currentUserId = _currentUserService.UserId
                 ?? throw new UnauthorizedException();

            var booking = await _bookingService.CreateBookingAsync(dto.EventId, currentUserId, ct);

            return CreatedAtAction(
                nameof(GetById),
                new { id = booking.Id },
                booking);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
               ?? throw new UnauthorizedException();

            if (!_currentUserService.IsAdmin)
            {
                throw new AccessDeniedException("У вас нет прав на отмену этой брони.");
            }

            await _bookingService.CancelBookingAsync(id, cancellationToken);
            return NoContent();
        }
    }

}
