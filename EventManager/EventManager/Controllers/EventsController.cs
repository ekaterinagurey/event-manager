using EventManager.Application.DTOs.Bookings;
using EventManager.Application.DTOs.Events;
using EventManager.Application.Mappers;
using EventManager.Application.Services.Interfaces;
using EventManager.Domain.Models;
using EventManager.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventManager.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly IBookingService _bookingService;
        public EventsController(IEventService eventService, IBookingService bookingService)
        {
            _eventService = eventService;
            _bookingService = bookingService;
        }

        [HttpGet]
        public async Task<ActionResult<PaginateResultDTO<Event>>> GetAll([FromQuery] GetEventsRequestDTO filter)
        {
            return Ok(await _eventService.GetEventsAsync(filter));
        }

        [HttpGet("{id:Guid}")]
        public async Task<ActionResult<Event>> GetById(Guid id)
        {
            return Ok(await _eventService.GetEventByIdAsync(id));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<EventInfoDTO>> Create([FromBody] CreateEventDTO newEvent)
        {
            var result = await _eventService.CreateEventAsync(newEvent);
            return CreatedAtAction(nameof(GetById),
                                   new { id = result.Id },
                                   result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<EventInfoDTO>> UpdateEvent(Guid id, [FromBody] UpdateEventDTO editingEvent)
        {
            var result = await _eventService.UpdateEventAsync(id, editingEvent);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:Guid}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var result = await _eventService.RemoveEventAsync(id);
            return NoContent();
        }

        [Authorize]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [HttpPost("{id:guid}/book")]
        public async Task<ActionResult<BookingResponseDTO>> Book(Guid id)
        {
            var userId = User.GetUserId();

            var booking = await _bookingService.CreateBookingAsync(id, userId);
            return AcceptedAtAction(nameof(BookingsController.GetById),
                                    "Bookings",
                                    new { id = booking.Id },
                                    booking.ToResponse());
        }
    }
}
