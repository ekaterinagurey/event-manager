using EventManager.Domain.Models;
using EventManager.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EventManager.Extensions;
using EventManager.Domain.Enums;

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

        [Authorize]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Booking>> GetById(Guid id)
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);
            return Ok(booking);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var role = User.IsInRole("Admin")
                ? UserRole.Admin
                : UserRole.User;

            await _bookingService.CancelBookingAsync(id, userId, role, cancellationToken);
            return NoContent();
        }
    }

}
