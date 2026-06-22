namespace EventManager.DTOs
{
    public class GetEventsRequestDTO
    {
        public string? Title { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
