namespace EventManager.DTOs.Events
{
    public class UpdateEventDTO
    {
        public string? Title { get; init; }
        public DateTime? StartAt { get; init; }
        public DateTime? EndAt { get; init; }
        public string? Description { get; init; }

    }
}
