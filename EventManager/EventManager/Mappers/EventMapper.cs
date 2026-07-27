using EventManager.DTOs.Events;
using EventManager.Models;

namespace EventManager.Mappers
{
    public static class EventMapper
    {
        public static Event ToEntity(this CreateEventDTO eventDTO)
        {
            return new Event
            {
                Title = eventDTO.Title,
                Description = eventDTO.Description,
                StartAt = eventDTO.StartAt,
                EndAt = eventDTO.EndAt,
                TotalSeats = eventDTO.TotalSeats
            };
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
            return new Event
            {
                Id = eventDTO.Id,
                Title = eventDTO.Title,
                Description = eventDTO.Description,
                StartAt = eventDTO.StartAt,
                EndAt = eventDTO.EndAt,
                TotalSeats = eventDTO.TotalSeats,
                AvailableSeats = eventDTO.AvailableSeats
            };
        }
    }
}
