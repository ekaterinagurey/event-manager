using EventManager.Application.DTOs.Users;
using EventManager.Domain.Enums;
using EventManager.Domain.Models;
using EventManager.Infrastructure.DataAccess;
using EventManager.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace EventManager.IntegrationTests
{
    public class AuthTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;

        public AuthTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }


        //Вспомогательный метод для создания пользователя
        private async Task<string?> CreateUserAsync(HttpClient client,
                                                    string userName,
                                                    string password)
        {
            await client.PostAsJsonAsync("/auth/register",
                                         new
                                         {
                                             Login = userName,
                                             Password = password
                                         });

            var tokenResponse = await client.PostAsJsonAsync("/auth/login",
                                                        new
                                                        {
                                                            Login = userName,
                                                            Password = password
                                                        });

            var result = await tokenResponse.Content.ReadFromJsonAsync<JsonObject>();
            var tokenAdmin = result["token"]?.ToString();
            return tokenAdmin;
        }

        // Вспомогательный метод создания пользователя с ролью Admin
        private async Task<string> CreateAdminAsync(HttpClient client,
                                                    string login,
                                                    string password)
        {
            using var scope = _fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            var passwordHash = Convert.ToHexString(hashBytes);
            var admin = User.Create(login, passwordHash, UserRole.Admin);

            context.Users.Add(admin);
            await context.SaveChangesAsync();

            var loginRequestDTO = new LoginRequestDTO { Login = login, Password = password };
            var response = await client.PostAsJsonAsync("/auth/login", loginRequestDTO);
            var body = await response.Content.ReadFromJsonAsync<JsonObject>();
            return body?["token"]?.ToString();
        }

        //Тест для проверки метода POST /auth/login с валидными данными 
        [Fact]
        public async Task Login_ValidCredentials_Returns200AndToken()
        {
            // Arrange
            using var client = _fixture.Factory.CreateClient();
            await client.PostAsJsonAsync("/auth/register",
                                         new
                                         {
                                             Login = "testuser",
                                             Password = "Password123!"
                                         });

            // Act
            var response = await client.PostAsJsonAsync("/auth/login",
                                                        new
                                                        {
                                                            Login = "testuser",
                                                            Password = "Password123!"
                                                        });

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<JsonObject>();
            Assert.NotNull(result);

            var token = result["token"]?.ToString();
            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        //Тест для проверки метода POST /auth/login с неверным паролем 
        [Fact]
        public async Task Login_InvalidPassword_Returns401Unauthorized()
        {
            // Arrange
            using var client = _fixture.Factory.CreateClient();
            await client.PostAsJsonAsync("/auth/register",
                                         new
                                         {
                                             Login = "testuser",
                                             Password = "Password123!"
                                         });

            // Act
            var response = await client.PostAsJsonAsync("/auth/login",
                                                        new
                                                        {
                                                            Login = "testuser",
                                                            Password = "WRONG_PASSWORD!"
                                                        });

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        //Тест для проверки запроса к защищенному эндпоинту без токена
        [Fact]
        public async Task CreateBooking_WithoutToken_Returns401Unauthorized()
        {
            // Arrange
            using var client = _fixture.Factory.CreateClient();
            var eventId = Guid.NewGuid();

            // Act
            var response = await client.PostAsJsonAsync($"/events/{eventId}/book", new { });

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        //Тест для проверки Создание события пользователем без роли Admin
        [Fact]
        public async Task CreateEvent_AsRegularUser_Returns403Forbidden()
        {
            // Arrange
            using var client = _fixture.Factory.CreateClient();
            var token = await CreateUserAsync(client, "testuser", "Password123!");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var newEvent = new
            {
                Title = "Test",
                StartAt = DateTime.UtcNow.AddDays(5),
                EndAt = DateTime.UtcNow.AddDays(5).AddHours(3),
                TotalSeats = 10
            };

            // Act
            var response = await client.PostAsJsonAsync("/events", newEvent);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        //Тест для проверки отмены чужой брони обычным пользователем
        [Fact]
        public async Task CancelBooking_OwnedByAnotherUser_Returns403Forbidden()
        {
            // Arrange
            using var client = _fixture.Factory.CreateClient();
            var tokenAdmin = await CreateAdminAsync(client, "admin777", "Password123!");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);

            var eventResponse = await client.PostAsJsonAsync("/events", new
            {
                Title = "Test event",
                StartAt = DateTime.UtcNow.AddDays(2),
                EndAt = DateTime.UtcNow.AddDays(2).AddHours(2),
                TotalSeats = 10
            });

            var eventObj = await eventResponse.Content.ReadFromJsonAsync<JsonObject>();
            var eventId = Guid.Parse(eventObj!["id"]!.ToString());

            var tokenUser1 = await CreateUserAsync(client, "user1", "Password123!");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenUser1);

            var bookingResponse = await client.PostAsJsonAsync($"/events/{eventId}/book", new { });
            var bookingObj = await bookingResponse.Content.ReadFromJsonAsync<JsonObject>();
            var bookingId = Guid.Parse(bookingObj!["id"]!.ToString());

            var tokenUser2 = await CreateUserAsync(client, "user2", "Password123!");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenUser2);

            // Act
            var deleteResponse = await client.DeleteAsync($"/bookings/{bookingId}");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
        }

        //Тест для проверки попытки зарегистрироваться с Role=Admin через Body
        [Fact]
        public async Task Register_WithAdminRoleInPayload_ShouldReject()
        {
            // Arrange
            using var client = _fixture.Factory.CreateClient();

            var payloadWithAdminRole = new
            {
                Username = "user1",
                Password = "Password123!",
                Role = "Admin"
            };

            // Act
            var response = await client.PostAsJsonAsync("/auth/register", payloadWithAdminRole);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
