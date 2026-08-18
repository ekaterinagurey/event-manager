using System;
using System.ComponentModel.DataAnnotations;

namespace EventManager.Models
{
    public class Booking
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }

        public Event Event { get; private set; } = null!;

        private Booking()
        {
        }

        private Booking(Guid id,
                        Guid eventId,
                        BookingStatus status,
                        DateTime createdAt)
        {
            Id = id;
            EventId = eventId;
            Status = status;
            CreatedAt = createdAt;
        }

        public static Booking Create(Guid eventId)
        {
            if (eventId == Guid.Empty)
                throw new ValidationException("EventId не может быть пустым.");

            return new Booking(Guid.NewGuid(),
                               eventId,
                               BookingStatus.Pending,
                               DateTime.UtcNow);
        }


        public void Confirm()
        {
            Status = BookingStatus.Confirmed;
            ProcessedAt = DateTime.UtcNow;
        }
        public void Reject()
        {
            Status = BookingStatus.Rejected;
            ProcessedAt = DateTime.UtcNow;
        }
    }
}
