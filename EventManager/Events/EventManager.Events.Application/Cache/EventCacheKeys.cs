namespace EventManager.Events.Application.Cache
{
    public class EventCacheKeys
    {
        public static string EventById(Guid id) => $"event:{id}";

        public const string Top10 = "events:top10";
    }
}
