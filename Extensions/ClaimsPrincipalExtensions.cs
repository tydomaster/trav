using System.Security.Claims;
using TravelPlanner.Api.Models;

namespace TravelPlanner.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public static long? GetTelegramId(this ClaimsPrincipal user)
    {
        var telegramIdClaim = user.FindFirst("TelegramId")?.Value;
        return long.TryParse(telegramIdClaim, out var telegramId) ? telegramId : null;
    }
}

