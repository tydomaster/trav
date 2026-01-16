using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Data;
using TravelPlanner.Api.DTOs;
using TravelPlanner.Api.Extensions;
using TravelPlanner.Api.Models;
using TravelPlanner.Api.Services;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Route("api/trips/{tripId}/assistant")]
public class AssistantController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILlmProvider _llmProvider;

    public AssistantController(ApplicationDbContext context, ILlmProvider llmProvider)
    {
        _context = context;
        _llmProvider = llmProvider;
    }

    // POST: api/trips/{tripId}/assistant/message
    [HttpPost("message")]
    public async Task<ActionResult<AssistantMessageDto>> SendMessage(int tripId, SendMessageDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке
        var hasAccess = await _context.Memberships
            .AnyAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (!hasAccess)
            return StatusCode(403, new { error = "Access denied", message = "You are not a member of this trip" });

        var response = await _llmProvider.SendMessageAsync(tripId, dto.Message);

        return Ok(response);
    }

    // POST: api/trips/{tripId}/assistant/apply
    [HttpPost("apply")]
    public async Task<IActionResult> ApplySuggestion(int tripId, ApplySuggestionDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке (только editor и owner могут применять)
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null || 
            (membership.Role != MembershipRole.Owner && membership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can apply suggestions" });

        // Здесь можно добавить логику применения предложения
        // Например, создание itinerary items или list items на основе suggestionId
        // Для MVP просто возвращаем успех

        return Ok(new { message = "Suggestion applied successfully" });
    }
}
