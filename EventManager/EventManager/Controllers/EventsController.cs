using EventManager.DTOs.Bookings;
using EventManager.DTOs.Events;
using EventManager.Interfaces;
using EventManager.Mappers;
using EventManager.Models;
using EventManager.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Net;

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

        [HttpPost]
        public async Task<ActionResult<EventInfoDTO>> Create([FromBody] CreateEventDTO newEvent)
        {
            var result = await _eventService.CreateEventAsync(newEvent);
            return CreatedAtAction(nameof(GetById),
                                   new { id = result.Id },
                                   result);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<EventInfoDTO>> UpdateEvent(Guid id, [FromBody] CreateEventDTO newEvent)
        {
            var result = await _eventService.UpdateEventAsync(id, newEvent.ToEntity());
            return Ok(result);
        }

        [HttpDelete("{id:Guid}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var result = await _eventService.RemoveEventAsync(id);
            return NoContent();
        }

        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [HttpPost("{id:guid}/book")]
        public async Task<ActionResult<BookingResponseDTO>> Book(Guid id)
        {
            var booking = await _bookingService.CreateBookingAsync(id);
            return AcceptedAtAction(nameof(BookingsController.GetById), 
                                    "Bookings", 
                                    new { id = booking.Id },
                                    booking.ToResponse());
        }
    }
}
