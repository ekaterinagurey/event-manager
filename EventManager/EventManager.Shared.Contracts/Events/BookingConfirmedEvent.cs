namespace EventManager.Shared.Contracts.Events
{
    public record BookingConfirmedEvent
    {
        public Guid BookingId { get; init; }
        public Guid EventId { get; init; }
        public Guid UserId { get; init; }
        public int SeatsCount { get; init; }
        public DateTime ConfirmedAtUtc { get; init; }

        public BookingConfirmedEvent() { }

        public BookingConfirmedEvent(
            Guid bookingId,
            Guid eventId,
            Guid userId,
            int seatsCount,
            DateTime confirmedAtUtc)
        {
            BookingId = bookingId;
            EventId = eventId;
            UserId = userId;
            SeatsCount = seatsCount;
            ConfirmedAtUtc = confirmedAtUtc;
        }
    }
}
