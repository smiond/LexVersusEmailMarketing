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
public class TemplatesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<EmailTemplateDto>>> GetAll(CancellationToken ct)
    {
        var templates = await db.EmailTemplates
            .OrderByDescending(t => t.UpdatedAt)
            .Select(t => new EmailTemplateDto(t.Id, t.Name, t.Subject, t.HtmlBody, t.CreatedAt, t.UpdatedAt))
            .ToListAsync(ct);

        return templates;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmailTemplateDto>> GetById(Guid id, CancellationToken ct)
    {
        var template = await db.EmailTemplates.FindAsync([id], ct);
        if (template is null) return NotFound();

        return new EmailTemplateDto(template.Id, template.Name, template.Subject, template.HtmlBody, template.CreatedAt, template.UpdatedAt);
    }

    [HttpPost]
    public async Task<ActionResult<EmailTemplateDto>> Create(CreateEmailTemplateDto dto, CancellationToken ct)
    {
        var template = new EmailTemplate
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Subject = dto.Subject.Trim(),
            HtmlBody = dto.HtmlBody
        };

        db.EmailTemplates.Add(template);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = template.Id },
            new EmailTemplateDto(template.Id, template.Name, template.Subject, template.HtmlBody, template.CreatedAt, template.UpdatedAt));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EmailTemplateDto>> Update(Guid id, UpdateEmailTemplateDto dto, CancellationToken ct)
    {
        var template = await db.EmailTemplates.FindAsync([id], ct);
        if (template is null) return NotFound();

        template.Name = dto.Name.Trim();
        template.Subject = dto.Subject.Trim();
        template.HtmlBody = dto.HtmlBody;
        template.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return new EmailTemplateDto(template.Id, template.Name, template.Subject, template.HtmlBody, template.CreatedAt, template.UpdatedAt);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var template = await db.EmailTemplates.FindAsync([id], ct);
        if (template is null) return NotFound();

        var inUse = await db.Campaigns.AnyAsync(c => c.EmailTemplateId == id, ct);
        if (inUse)
        {
            return Conflict("Template is used by one or more campaigns.");
        }

        db.EmailTemplates.Remove(template);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
