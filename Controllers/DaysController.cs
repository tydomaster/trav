using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Data;
using TravelPlanner.Api.DTOs;
using TravelPlanner.Api.Extensions;
using TravelPlanner.Api.Models;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Route("api/trips/{tripId}/days")]
public class DaysController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DaysController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/trips/{tripId}/days - Получить все дни поездки
    [HttpGet]
    public async Task<ActionResult<List<DayDto>>> GetDays(int tripId)
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

        var days = await _context.Days
            .Include(d => d.Items.OrderBy(i => i.Order))
            .Where(d => d.TripId == tripId)
            .OrderBy(d => d.Date)
            .Select(d => new DayDto
            {
                Id = d.Id,
                TripId = d.TripId,
                Date = d.Date,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                Items = d.Items.Select(i => new ItemDto
                {
                    Id = i.Id,
                    DayId = i.DayId,
                    StartTime = i.StartTime.HasValue ? i.StartTime.Value.ToString(@"hh\:mm") : null,
                    DurationMinutes = i.DurationMinutes,
                    Title = i.Title,
                    PlaceId = i.PlaceId,
                    Notes = i.Notes,
                    Order = i.Order,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt
                }).ToList()
            })
            .ToListAsync();

        return Ok(days);
    }

    // GET: api/trips/{tripId}/days/{dayId} - Получить день
    [HttpGet("{dayId}")]
    public async Task<ActionResult<DayDto>> GetDay(int tripId, int dayId)
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

        var day = await _context.Days
            .Include(d => d.Items.OrderBy(i => i.Order))
            .FirstOrDefaultAsync(d => d.Id == dayId && d.TripId == tripId);

        if (day == null)
            return NotFound();

        var dayDto = new DayDto
        {
            Id = day.Id,
            TripId = day.TripId,
            Date = day.Date,
            CreatedAt = day.CreatedAt,
            UpdatedAt = day.UpdatedAt,
            Items = day.Items.Select(i => new ItemDto
            {
                Id = i.Id,
                DayId = i.DayId,
                StartTime = i.StartTime.HasValue ? i.StartTime.Value.ToString(@"hh\:mm") : null,
                DurationMinutes = i.DurationMinutes,
                Title = i.Title,
                PlaceId = i.PlaceId,
                Notes = i.Notes,
                Order = i.Order,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt
            }).ToList()
        };

        return Ok(dayDto);
    }

    // POST: api/trips/{tripId}/days - Создать день
    [HttpPost]
    public async Task<ActionResult<DayDto>> CreateDay(int tripId, CreateDayDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке (только editor и owner могут создавать дни)
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null || 
            (membership.Role != MembershipRole.Owner && membership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can modify days" });

        // Проверяем, что день с такой датой еще не существует
        var existingDay = await _context.Days
            .FirstOrDefaultAsync(d => d.TripId == tripId && d.Date.Date == dto.Date.Date);

        if (existingDay != null)
            return Conflict(new { error = "Day with this date already exists" });

        var day = new Day
        {
            TripId = tripId,
            Date = dto.Date.Date,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Days.Add(day);
        await _context.SaveChangesAsync();

        var dayDto = new DayDto
        {
            Id = day.Id,
            TripId = day.TripId,
            Date = day.Date,
            CreatedAt = day.CreatedAt,
            UpdatedAt = day.UpdatedAt,
            Items = new List<ItemDto>()
        };

        return CreatedAtAction(nameof(GetDay), new { tripId, dayId = day.Id }, dayDto);
    }

    // DELETE: api/trips/{tripId}/days/{dayId} - Удалить день
    [HttpDelete("{dayId}")]
    public async Task<IActionResult> DeleteDay(int tripId, int dayId)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке (только editor и owner могут удалять дни)
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null || 
            (membership.Role != MembershipRole.Owner && membership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can modify days" });

        var day = await _context.Days
            .FirstOrDefaultAsync(d => d.Id == dayId && d.TripId == tripId);

        if (day == null)
            return NotFound();

        _context.Days.Remove(day);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

