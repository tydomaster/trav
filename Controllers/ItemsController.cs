using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Data;
using TravelPlanner.Api.DTOs;
using TravelPlanner.Api.Extensions;
using TravelPlanner.Api.Models;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Route("api/trips/{tripId}/days/{dayId}/items")]
public class ItemsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ItemsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // POST: api/trips/{tripId}/days/{dayId}/items - Создать item
    [HttpPost]
    public async Task<ActionResult<ItemDto>> CreateItem(int tripId, int dayId, CreateItemDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null || 
            (membership.Role != MembershipRole.Owner && membership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can modify items" });

        // Проверяем, что день существует и принадлежит поездке
        var day = await _context.Days
            .FirstOrDefaultAsync(d => d.Id == dayId && d.TripId == tripId);

        if (day == null)
            return NotFound();

        // Парсим время
        TimeSpan? startTime = null;
        if (!string.IsNullOrEmpty(dto.StartTime) && TimeSpan.TryParse(dto.StartTime, out var parsedTime))
        {
            startTime = parsedTime;
        }

        var item = new Item
        {
            DayId = dayId,
            StartTime = startTime,
            DurationMinutes = dto.DurationMinutes,
            Title = dto.Title,
            PlaceId = dto.PlaceId,
            Notes = dto.Notes,
            Order = dto.Order,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        var itemDto = new ItemDto
        {
            Id = item.Id,
            DayId = item.DayId,
            StartTime = item.StartTime.HasValue ? item.StartTime.Value.ToString(@"hh\:mm") : null,
            DurationMinutes = item.DurationMinutes,
            Title = item.Title,
            PlaceId = item.PlaceId,
            Notes = item.Notes,
            Order = item.Order,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };

        return CreatedAtAction(nameof(GetItem), new { tripId, dayId, itemId = item.Id }, itemDto);
    }

    // GET: api/trips/{tripId}/days/{dayId}/items/{itemId} - Получить item
    [HttpGet("{itemId}")]
    public async Task<ActionResult<ItemDto>> GetItem(int tripId, int dayId, int itemId)
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

        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.Id == itemId && i.DayId == dayId);

        if (item == null)
            return NotFound();

        var itemDto = new ItemDto
        {
            Id = item.Id,
            DayId = item.DayId,
            StartTime = item.StartTime.HasValue ? item.StartTime.Value.ToString(@"hh\:mm") : null,
            DurationMinutes = item.DurationMinutes,
            Title = item.Title,
            PlaceId = item.PlaceId,
            Notes = item.Notes,
            Order = item.Order,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };

        return Ok(itemDto);
    }

    // PUT: api/trips/{tripId}/days/{dayId}/items/{itemId} - Обновить item
    [HttpPut("{itemId}")]
    public async Task<ActionResult<ItemDto>> UpdateItem(int tripId, int dayId, int itemId, UpdateItemDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null || 
            (membership.Role != MembershipRole.Owner && membership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can modify items" });

        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.Id == itemId && i.DayId == dayId);

        if (item == null)
            return NotFound();

        // Парсим время
        TimeSpan? startTime = null;
        if (!string.IsNullOrEmpty(dto.StartTime) && TimeSpan.TryParse(dto.StartTime, out var parsedTime))
        {
            startTime = parsedTime;
        }

        item.StartTime = startTime;
        item.DurationMinutes = dto.DurationMinutes;
        item.Title = dto.Title;
        item.PlaceId = dto.PlaceId;
        item.Notes = dto.Notes;
        item.Order = dto.Order;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var itemDto = new ItemDto
        {
            Id = item.Id,
            DayId = item.DayId,
            StartTime = item.StartTime.HasValue ? item.StartTime.Value.ToString(@"hh\:mm") : null,
            DurationMinutes = item.DurationMinutes,
            Title = item.Title,
            PlaceId = item.PlaceId,
            Notes = item.Notes,
            Order = item.Order,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };

        return Ok(itemDto);
    }

    // DELETE: api/trips/{tripId}/days/{dayId}/items/{itemId} - Удалить item
    [HttpDelete("{itemId}")]
    public async Task<IActionResult> DeleteItem(int tripId, int dayId, int itemId)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null || 
            (membership.Role != MembershipRole.Owner && membership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can modify items" });

        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.Id == itemId && i.DayId == dayId);

        if (item == null)
            return NotFound();

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PUT: api/trips/{tripId}/days/{dayId}/items/reorder - Изменить порядок items
    [HttpPut("reorder")]
    public async Task<IActionResult> ReorderItems(int tripId, int dayId, ReorderItemsDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null || 
            (membership.Role != MembershipRole.Owner && membership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can modify items" });

        // Проверяем, что день существует и принадлежит поездке
        var day = await _context.Days
            .FirstOrDefaultAsync(d => d.Id == dayId && d.TripId == tripId);

        if (day == null)
            return NotFound();

        // Обновляем порядок items
        var items = await _context.Items
            .Where(i => i.DayId == dayId && dto.ItemIds.Contains(i.Id))
            .ToListAsync();

        if (items.Count != dto.ItemIds.Count)
            return BadRequest(new { error = "Some items not found" });

        for (int i = 0; i < dto.ItemIds.Count; i++)
        {
            var item = items.First(it => it.Id == dto.ItemIds[i]);
            item.Order = i;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }
}

