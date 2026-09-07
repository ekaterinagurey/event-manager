using EventManager.Domain.Enums;
using EventManager.Domain.Models;
using EventManager.Infrastructure.DataAccess;
using EventManager.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EventManager.IntegrationTests
{
    public class UserRepositoryTests
    {
        private AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task AddAsync_ShouldSaveUserToDatabase()
        {
            // Arrange
            await using var context = CreateContext();
            var repository = new UserRepository(context);
            var user = User.Create("testuser", "hashed_secret", UserRole.User);

            // Act
            await repository.CreateAsync(user, CancellationToken.None);
            await context.SaveChangesAsync();

            // Assert
            await using var verifyContext = CreateContext();
            var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            Assert.NotNull(savedUser);
            Assert.Equal("testuser", savedUser.Login);
            Assert.Equal(UserRole.User, savedUser.Role);
        }

        [Fact]
        public async Task GetByEmailAsync_WhenUserExists_ShouldReturnUser()
        {
            // Arrange
            await using var context = CreateContext();
            var user = User.Create("testuser", "hashed_secret", UserRole.Admin);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var repository = new UserRepository(context);

            // Act
            var foundUser = await repository.GetByLoginAsync("testuser", CancellationToken.None);

            // Assert
            Assert.NotNull(foundUser);
            Assert.Equal(user.Id, foundUser.Id);
            Assert.Equal(UserRole.Admin, foundUser.Role);
        }

        [Fact]
        public async Task GetByEmailAsync_WhenUserDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            await using var context = CreateContext();
            var repository = new UserRepository(context);

            // Act
            var result = await repository.GetByLoginAsync("testuser", CancellationToken.None);

            // Assert
            Assert.Null(result);
        }
    }
}
