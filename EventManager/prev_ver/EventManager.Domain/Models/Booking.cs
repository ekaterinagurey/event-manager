using EventManager.Domain.Enums;
using EventManager.Domain.Exceptions;

namespace EventManager.Domain.Models
{
    public class Booking
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public Guid UserId { get; private set; }
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }

        public Event Event { get; private set; } = null!;

        private Booking()
        {
        }

        private Booking(Guid id,
                        Guid eventId,
                        Guid userId,
                        BookingStatus status,
                        DateTime createdAt)
        {
            Id = id;
            EventId = eventId;
            UserId = userId;
            Status = status;
            CreatedAt = createdAt;
        }

        public static Booking Create(Guid eventId, Guid userId)
        {
            if (eventId == Guid.Empty)
                throw new DomainValidationException("EventId не может быть пустым.");

            if (userId == Guid.Empty)
                throw new DomainValidationException("userId не может быть пустым.");

            return new Booking(Guid.NewGuid(),
                               eventId,
                               userId,
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

        public void Cancel()
        {
            // Защита от повторной отмены
            if (Status == BookingStatus.Cancelled)
                throw new InvalidOperationException("Booking is already cancelled.");

            Status = BookingStatus.Cancelled;
            ProcessedAt = DateTime.UtcNow;
        }
    }
}
