using System.Security.Claims;

namespace Training.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(value, out int userId))
            throw new UnauthorizedAccessException("Invalid user id");

        return userId;
    }
}