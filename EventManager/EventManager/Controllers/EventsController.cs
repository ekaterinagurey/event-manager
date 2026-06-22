using EventManager.Interfaces;
using EventManager.Models;
using EventManager.Mappers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using EventManager.DTOs;

namespace EventManager.Controllers
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
        public ActionResult<IEnumerable<Event>> GetAll([FromQuery] GetEventsRequestDTO filter)
        {
            // Получаем все 
            return Ok(_eventService.GetEvents(filter));
        }

        [HttpGet("{id:int}")]
        public ActionResult<Event> GetById(int id)
        {
            var result = _eventService.GetEvent(id);

            if (result is null)
                return NotFound();

            return Ok(result);
        }


        [HttpPost]
        public IActionResult Post([FromBody] EventDTO newEvent)
        {
            var result = _eventService.AddEvent(newEvent.ToEntity());
            return new CreatedResult($"/Events/{result.Id}", _eventService.GetEvent(result.Id));
        }

        [HttpPut("{id:int}")]
        public IActionResult Put(int id, [FromBody] EventDTO newEvent)
        {
            newEvent.Id = id;
            var result = _eventService.ChangeEvent(id, newEvent.ToEntity());
            if (!result)
                return NotFound();

            return Ok(_eventService.GetEvent(id));
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var result = _eventService.RemoveEvent(id);

            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}
