using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Data;
using TravelPlanner.Api.DTOs;
using TravelPlanner.Api.Extensions;
using TravelPlanner.Api.Models;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Route("api/trips/{tripId}/lists")]
public class ListsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ListsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/trips/{tripId}/lists?type=1
    [HttpGet]
    public async Task<ActionResult<List<ListDto>>> GetLists(int tripId, [FromQuery] int? type)
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

        var query = _context.Lists
            .Include(l => l.AddedBy)
            .Where(l => l.TripId == tripId);

        if (type.HasValue)
        {
            query = query.Where(l => l.Type == (ListType)type.Value);
        }

        var lists = await query
            .OrderBy(l => l.Order)
            .ThenBy(l => l.Completed)
            .Select(l => new ListDto
            {
                Id = l.Id,
                TripId = l.TripId,
                Type = (int)l.Type,
                Text = l.Text,
                Completed = l.Completed,
                AddedByUserId = l.AddedByUserId,
                AddedByName = l.AddedBy != null ? l.AddedBy.Name : null,
                Order = l.Order,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            })
            .ToListAsync();

        return Ok(lists);
    }

    // POST: api/trips/{tripId}/lists
    [HttpPost]
    public async Task<ActionResult<ListDto>> CreateListItem(int tripId, CreateListItemDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке (только editor и owner могут создавать)
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null || 
            (membership.Role != MembershipRole.Owner && membership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can modify lists" });

        var listItem = new List
        {
            TripId = tripId,
            Type = (ListType)dto.Type,
            Text = dto.Text,
            Completed = false,
            AddedByUserId = userIdValue,
            Order = dto.Order,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Lists.Add(listItem);
        await _context.SaveChangesAsync();

        await _context.Entry(listItem)
            .Reference(l => l.AddedBy)
            .LoadAsync();

        var listDto = new ListDto
        {
            Id = listItem.Id,
            TripId = listItem.TripId,
            Type = (int)listItem.Type,
            Text = listItem.Text,
            Completed = listItem.Completed,
            AddedByUserId = listItem.AddedByUserId,
            AddedByName = listItem.AddedBy?.Name,
            Order = listItem.Order,
            CreatedAt = listItem.CreatedAt,
            UpdatedAt = listItem.UpdatedAt
        };

        return CreatedAtAction(nameof(GetLists), new { tripId }, listDto);
    }

    // PUT: api/trips/{tripId}/lists/{listId}
    [HttpPut("{listId}")]
    public async Task<ActionResult<ListDto>> UpdateListItem(int tripId, int listId, UpdateListItemDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке (только editor и owner могут обновлять)
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null || 
            (membership.Role != MembershipRole.Owner && membership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can modify lists" });

        var listItem = await _context.Lists
            .FirstOrDefaultAsync(l => l.Id == listId && l.TripId == tripId);

        if (listItem == null)
            return NotFound();

        if (dto.Text != null)
            listItem.Text = dto.Text;
        if (dto.Completed.HasValue)
            listItem.Completed = dto.Completed.Value;
        if (dto.Order.HasValue)
            listItem.Order = dto.Order.Value;
        listItem.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _context.Entry(listItem)
            .Reference(l => l.AddedBy)
            .LoadAsync();

        var listDto = new ListDto
        {
            Id = listItem.Id,
            TripId = listItem.TripId,
            Type = (int)listItem.Type,
            Text = listItem.Text,
            Completed = listItem.Completed,
            AddedByUserId = listItem.AddedByUserId,
            AddedByName = listItem.AddedBy?.Name,
            Order = listItem.Order,
            CreatedAt = listItem.CreatedAt,
            UpdatedAt = listItem.UpdatedAt
        };

        return Ok(listDto);
    }

    // DELETE: api/trips/{tripId}/lists/{listId}
    [HttpDelete("{listId}")]
    public async Task<IActionResult> DeleteListItem(int tripId, int listId)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке (только editor и owner могут удалять)
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null || 
            (membership.Role != MembershipRole.Owner && membership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can modify lists" });

        var listItem = await _context.Lists
            .FirstOrDefaultAsync(l => l.Id == listId && l.TripId == tripId);

        if (listItem == null)
            return NotFound();

        _context.Lists.Remove(listItem);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PUT: api/trips/{tripId}/lists/reorder
    [HttpPut("reorder")]
    public async Task<IActionResult> ReorderListItems(int tripId, [FromQuery] int type, ReorderListItemsDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке (только editor и owner могут изменять порядок)
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null || 
            (membership.Role != MembershipRole.Owner && membership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can modify lists" });

        var listItems = await _context.Lists
            .Where(l => l.TripId == tripId && l.Type == (ListType)type && dto.ItemIds.Contains(l.Id))
            .ToListAsync();

        if (listItems.Count != dto.ItemIds.Count)
            return BadRequest(new { error = "Some items not found" });

        for (int i = 0; i < dto.ItemIds.Count; i++)
        {
            var item = listItems.First(it => it.Id == dto.ItemIds[i]);
            item.Order = i;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
