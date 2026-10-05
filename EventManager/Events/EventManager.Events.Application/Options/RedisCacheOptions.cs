namespace EventManager.Events.Application.Options
{
    public class RedisCacheOptions
    {
        public const string SectionName = "Redis";

        public string ConnectionString { get; set; } = "localhost:6379";
        public int EventByIdTtlMinutes { get; set; } = 10;
        public int TopEventsTtlMinutes { get; set; } = 2;
        public string? Password { get; set; }
        public int ConnectTimeoutMs { get; set; } = 5000;
        public int SyncTimeoutMs { get; set; } = 3000;
    }
}
