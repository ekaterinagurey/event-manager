using EventManager.DTOs.Events;
using EventManager.Models;

namespace EventManager.Mappers
{
    public static class EventMapper
    {
        public static Event ToEntity(this CreateEventDTO eventDTO)
        {
            return Event.Create(eventDTO.Title,
                                eventDTO.StartAt,
                                eventDTO.EndAt,
                                eventDTO.TotalSeats,
                                eventDTO.Description);

        }

        public static EventInfoDTO ToResponse(this Event currentEvent)
        {
            return new EventInfoDTO
            {
                Id = currentEvent.Id,
                Title = currentEvent.Title,
                Description = currentEvent.Description,
                StartAt = currentEvent.StartAt,
                EndAt = currentEvent.EndAt,
                TotalSeats = currentEvent.TotalSeats,
                AvailableSeats = currentEvent.AvailableSeats
            };
        }
        public static Event ToEntity(this EventInfoDTO eventDTO)
        {
            return Event.Create(eventDTO.Title,
                                eventDTO.StartAt,
                                eventDTO.EndAt,
                                (int)eventDTO.TotalSeats,
                                eventDTO.Description);
        }
    }
}
