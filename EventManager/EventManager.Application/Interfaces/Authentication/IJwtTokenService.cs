using EventManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Application.Interfaces.Authentication
{
    public interface IJwtTokenService
    {
        string GenerateToken(User user);
    }
}
