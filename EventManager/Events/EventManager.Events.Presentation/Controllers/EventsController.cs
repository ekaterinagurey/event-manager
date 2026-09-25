using EventManager.Events.Application.DTOs;
using EventManager.Events.Application.Interfaces;
using EventManager.Events.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Events.Presentation.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
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
    }
}
