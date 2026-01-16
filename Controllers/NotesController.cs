using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Data;
using TravelPlanner.Api.DTOs;
using TravelPlanner.Api.Extensions;
using TravelPlanner.Api.Models;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Route("api/trips/{tripId}/notes")]
public class NotesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public NotesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/trips/{tripId}/notes
    [HttpGet]
    public async Task<ActionResult<List<NoteDto>>> GetNotes(int tripId)
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

        var notes = await _context.Notes
            .Include(n => n.User)
            .Where(n => n.TripId == tripId)
            .OrderByDescending(n => n.UpdatedAt)
            .Select(n => new NoteDto
            {
                Id = n.Id,
                TripId = n.TripId,
                UserId = n.UserId,
                UserName = n.User.Name,
                UserAvatar = n.User.Avatar,
                Title = n.Title,
                Content = n.Content,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt
            })
            .ToListAsync();

        return Ok(notes);
    }

    // POST: api/trips/{tripId}/notes
    [HttpPost]
    public async Task<ActionResult<NoteDto>> CreateNote(int tripId, CreateNoteDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        // Проверяем доступ к поездке (только editor и owner могут создавать заметки)
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null || 
            (membership.Role != MembershipRole.Owner && membership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can create notes" });

        var note = new Note
        {
            TripId = tripId,
            UserId = userIdValue,
            Title = dto.Title,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        // Загружаем пользователя для DTO
        await _context.Entry(note)
            .Reference(n => n.User)
            .LoadAsync();

        var noteDto = new NoteDto
        {
            Id = note.Id,
            TripId = note.TripId,
            UserId = note.UserId,
            UserName = note.User.Name,
            UserAvatar = note.User.Avatar,
            Title = note.Title,
            Content = note.Content,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };

        return CreatedAtAction(nameof(GetNote), new { tripId, noteId = note.Id }, noteDto);
    }

    // GET: api/trips/{tripId}/notes/{noteId}
    [HttpGet("{noteId}")]
    public async Task<ActionResult<NoteDto>> GetNote(int tripId, int noteId)
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

        var note = await _context.Notes
            .Include(n => n.User)
            .FirstOrDefaultAsync(n => n.Id == noteId && n.TripId == tripId);

        if (note == null)
            return NotFound();

        var noteDto = new NoteDto
        {
            Id = note.Id,
            TripId = note.TripId,
            UserId = note.UserId,
            UserName = note.User.Name,
            UserAvatar = note.User.Avatar,
            Title = note.Title,
            Content = note.Content,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };

        return Ok(noteDto);
    }

    // PUT: api/trips/{tripId}/notes/{noteId}
    [HttpPut("{noteId}")]
    public async Task<ActionResult<NoteDto>> UpdateNote(int tripId, int noteId, UpdateNoteDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        var note = await _context.Notes
            .Include(n => n.User)
            .FirstOrDefaultAsync(n => n.Id == noteId && n.TripId == tripId);

        if (note == null)
            return NotFound();

        // Проверяем доступ: только автор заметки, owner или editor могут редактировать
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null)
            return StatusCode(403, new { error = "Access denied", message = "You are not a member of this trip" });

        // Только автор, owner или editor могут редактировать
        if (note.UserId != userIdValue && 
            membership.Role != MembershipRole.Owner && 
            membership.Role != MembershipRole.Editor)
            return StatusCode(403, new { error = "Access denied", message = "Only note author, owner or editor can update notes" });

        // Обновляем поля
        if (dto.Title != null)
            note.Title = dto.Title;
        if (dto.Content != null)
            note.Content = dto.Content;
        
        note.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var noteDto = new NoteDto
        {
            Id = note.Id,
            TripId = note.TripId,
            UserId = note.UserId,
            UserName = note.User.Name,
            UserAvatar = note.User.Avatar,
            Title = note.Title,
            Content = note.Content,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };

        return Ok(noteDto);
    }

    // DELETE: api/trips/{tripId}/notes/{noteId}
    [HttpDelete("{noteId}")]
    public async Task<IActionResult> DeleteNote(int tripId, int noteId)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        var note = await _context.Notes
            .FirstOrDefaultAsync(n => n.Id == noteId && n.TripId == tripId);

        if (note == null)
            return NotFound();

        // Проверяем доступ: только автор заметки, owner или editor могут удалять
        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null)
            return StatusCode(403, new { error = "Access denied", message = "You are not a member of this trip" });

        // Только автор, owner или editor могут удалять
        if (note.UserId != userIdValue && 
            membership.Role != MembershipRole.Owner && 
            membership.Role != MembershipRole.Editor)
            return StatusCode(403, new { error = "Access denied", message = "Only note author, owner or editor can delete notes" });

        _context.Notes.Remove(note);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
