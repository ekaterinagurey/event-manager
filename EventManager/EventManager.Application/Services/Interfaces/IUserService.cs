using EventManager.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Application.Services.Interfaces
{
    public interface IUserService
    {
        Task RegisterAsync(string login,
                           string password,
                           UserRole role,
                           CancellationToken cancellationToken);

        Task<string> LoginAsync(string login,
                                string password,
                                CancellationToken cancellationToken);
    }
}
