using System.Security.Claims;

namespace EventManager.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
                throw new UnauthorizedAccessException("user id claim is missing.");

            if (!Guid.TryParse(claim.Value, out var userId))
                throw new UnauthorizedAccessException("Invalid user id claim.");

            return userId;
        }
    }
}
