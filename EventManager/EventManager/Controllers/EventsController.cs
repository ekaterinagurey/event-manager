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
        public ActionResult<PaginateResultDTO<Event>> GetAll([FromQuery] GetEventsRequestDTO filter)
        {
            return Ok(_eventService.GetEvents(filter));
        }

        [HttpGet("{id:int}")]
        public ActionResult<Event> GetById(Guid id)
        {
            var result = _eventService.GetEvent(id);
            return Ok(result);
        }


        [HttpPost]
        public IActionResult Post([FromBody] EventDTO newEvent)
        {
            var result = _eventService.AddEvent(newEvent.ToEntity());
            return new CreatedResult($"/Events/{result.Id}", _eventService.GetEvent(result.Id));
        }

        [HttpPut("{id:guid}")]
        public IActionResult Put(Guid id, [FromBody] EventDTO newEvent)
        {
            newEvent.Id = id;
            var result = _eventService.ChangeEvent(id, newEvent.ToEntity());
            return Ok(_eventService.GetEvent(id));
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(Guid id)
        {
            var result = _eventService.RemoveEvent(id);
            return NoContent();
        }

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
