namespace EventManager.Infrastructure.Authentication
{
    public class JwtOptions
    {
        public string Secret { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public int Expires { get; set; } = 15; // Время жизни токена в минутах
    }
}
