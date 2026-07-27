using EventManager.DTOs.Events;
using EventManager.Models;

namespace EventManager.Mappers
{
    public static class EventMapper
    {
        public static Event ToEntity(this EventDTO eventDTO)
        {
            return new Event
            {
                Title = eventDTO.Title,
                Description = eventDTO.Description,
                StartAt = eventDTO.StartAt,
                EndAt = eventDTO.EndAt
            };
        }
    }
}
