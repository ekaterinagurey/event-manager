namespace EventManager.Events.Domain.Entities
{
    public class ProcessedBooking
    {
        public Guid BookingId { get; set; }
        public DateTime ProcessedAtUtc { get; set; }
    }
}
