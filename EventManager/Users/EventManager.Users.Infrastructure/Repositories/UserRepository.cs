using EventManager.Users.Domain.Entities;
using EventManager.Users.Domain.Repositories;
using EventManager.Users.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace EventManager.Users.Infrastructure.Repositories
{
    public class UserRepository: IUserRepository
    {
        private readonly UserDbContext _context;

        public UserRepository(UserDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Login == login, cancellationToken);
        }

        public async Task CreateAsync(User newUser, CancellationToken cancellationToken)
        {
            await _context.Users.AddAsync(newUser, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
