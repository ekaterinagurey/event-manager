using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Infrastructure.Authentication
{
    public class JwtOptions
    {
        public string Secret { get; init; } = null!;
        public string Issuer { get; init; } = null!;
        public string Audience { get; init; } = null!;
        public DateTime Expires { get; init; } = DateTime.UtcNow.AddMinutes(15);
    }
}
