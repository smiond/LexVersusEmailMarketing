using LexVersusEmailMarketing.Core.DTOs;
using LexVersusEmailMarketing.Core.Entities;
using LexVersusEmailMarketing.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LexVersusEmailMarketing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SendersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SenderIdentityDto>>> GetAll([FromQuery] bool activeOnly = false, CancellationToken ct = default)
    {
        var query = db.SenderIdentities.AsQueryable();
        if (activeOnly)
        {
            query = query.Where(s => s.IsActive);
        }

        var senders = await query
            .OrderByDescending(s => s.IsDefault)
            .ThenBy(s => s.DisplayName)
            .Select(s => ToDto(s))
            .ToListAsync(ct);

        return senders;
    }

    [HttpPost]
    public async Task<ActionResult<SenderIdentityDto>> Create(CreateSenderIdentityDto dto, CancellationToken ct)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await db.SenderIdentities.AnyAsync(s => s.Email == email, ct))
        {
            return Conflict("Sender with this email already exists.");
        }

        if (dto.IsDefault)
        {
            await ClearDefaultAsync(ct);
        }

        var sender = new SenderIdentity
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = dto.DisplayName.Trim(),
            ReplyToEmail = NormalizeOptionalEmail(dto.ReplyToEmail),
            IsDefault = dto.IsDefault || !await db.SenderIdentities.AnyAsync(ct),
            IsActive = true
        };

        db.SenderIdentities.Add(sender);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetAll), ToDto(sender));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SenderIdentityDto>> Update(Guid id, UpdateSenderIdentityDto dto, CancellationToken ct)
    {
        var sender = await db.SenderIdentities.FindAsync([id], ct);
        if (sender is null) return NotFound();

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await db.SenderIdentities.AnyAsync(s => s.Email == email && s.Id != id, ct))
        {
            return Conflict("Sender with this email already exists.");
        }

        if (dto.IsDefault)
        {
            await ClearDefaultAsync(ct);
        }

        sender.Email = email;
        sender.DisplayName = dto.DisplayName.Trim();
        sender.ReplyToEmail = NormalizeOptionalEmail(dto.ReplyToEmail);
        sender.IsDefault = dto.IsDefault;
        sender.IsActive = dto.IsActive;

        if (!sender.IsDefault && !await db.SenderIdentities.AnyAsync(s => s.IsDefault && s.Id != id, ct))
        {
            sender.IsDefault = true;
        }

        await db.SaveChangesAsync(ct);
        return ToDto(sender);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var sender = await db.SenderIdentities.FindAsync([id], ct);
        if (sender is null) return NotFound();

        var inUse = await db.Campaigns.AnyAsync(c => c.SenderIdentityId == id, ct);
        if (inUse)
        {
            return Conflict("Sender is used by one or more campaigns. Deactivate it instead of deleting.");
        }

        var wasDefault = sender.IsDefault;
        db.SenderIdentities.Remove(sender);
        await db.SaveChangesAsync(ct);

        if (wasDefault)
        {
            var next = await db.SenderIdentities.Where(s => s.IsActive).OrderBy(s => s.CreatedAt).FirstOrDefaultAsync(ct);
            if (next is not null)
            {
                next.IsDefault = true;
                await db.SaveChangesAsync(ct);
            }
        }

        return NoContent();
    }

    private async Task ClearDefaultAsync(CancellationToken ct)
    {
        var defaults = await db.SenderIdentities.Where(s => s.IsDefault).ToListAsync(ct);
        foreach (var item in defaults)
        {
            item.IsDefault = false;
        }
    }

    private static string? NormalizeOptionalEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private static SenderIdentityDto ToDto(SenderIdentity s) =>
        new(s.Id, s.Email, s.DisplayName, s.ReplyToEmail, s.IsDefault, s.IsActive, s.CreatedAt);
}
