using LexVersusEmailMarketing.Core.DTOs;
using LexVersusEmailMarketing.Core.Entities;
using LexVersusEmailMarketing.Core.Interfaces;
using LexVersusEmailMarketing.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LexVersusEmailMarketing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactsController(AppDbContext db, IContactImportService importService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ContactDto>>> GetAll([FromQuery] string? search, CancellationToken ct)
    {
        var query = db.Contacts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(c =>
                c.Email.Contains(term) ||
                (c.FirstName != null && c.FirstName.ToLower().Contains(term)) ||
                (c.LastName != null && c.LastName.ToLower().Contains(term)));
        }

        var contacts = await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ContactDto(c.Id, c.Email, c.FirstName, c.LastName, c.IsSubscribed, c.CreatedAt))
            .ToListAsync(ct);

        return contacts;
    }

    [HttpPost]
    public async Task<ActionResult<ContactDto>> Create(CreateContactDto dto, CancellationToken ct)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await db.Contacts.AnyAsync(c => c.Email == email, ct))
        {
            return Conflict("Contact with this email already exists.");
        }

        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = dto.FirstName?.Trim(),
            LastName = dto.LastName?.Trim()
        };

        db.Contacts.Add(contact);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetAll), new ContactDto(
            contact.Id, contact.Email, contact.FirstName, contact.LastName, contact.IsSubscribed, contact.CreatedAt));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var contact = await db.Contacts.FindAsync([id], ct);
        if (contact is null) return NotFound();

        db.Contacts.Remove(contact);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ImportResultDto>> Import(
        IFormFile file,
        [FromForm] Guid? emailListId,
        CancellationToken ct)
    {
        if (file.Length == 0)
        {
            return BadRequest("File is empty.");
        }

        var allowed = new[] { ".csv", ".xlsx", ".xls" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
        {
            return BadRequest("Only CSV and Excel files are supported.");
        }

        await using var stream = file.OpenReadStream();
        var result = await importService.ImportFromFileAsync(stream, file.FileName, emailListId, ct);
        return Ok(result);
    }

    [HttpGet("lists")]
    public async Task<ActionResult<List<EmailListDto>>> GetLists(CancellationToken ct)
    {
        var lists = await db.EmailLists
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new EmailListDto(
                l.Id,
                l.Name,
                l.Description,
                l.EmailListContacts.Count,
                l.CreatedAt))
            .ToListAsync(ct);

        return lists;
    }

    [HttpPost("lists")]
    public async Task<ActionResult<EmailListDto>> CreateList(CreateEmailListDto dto, CancellationToken ct)
    {
        var list = new EmailList
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim()
        };

        db.EmailLists.Add(list);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetLists), new EmailListDto(list.Id, list.Name, list.Description, 0, list.CreatedAt));
    }

    [HttpDelete("lists/{id:guid}")]
    public async Task<IActionResult> DeleteList(Guid id, CancellationToken ct)
    {
        var list = await db.EmailLists.FindAsync([id], ct);
        if (list is null) return NotFound();

        db.EmailLists.Remove(list);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
