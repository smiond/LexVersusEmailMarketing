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
public class CampaignsController(AppDbContext db, ICampaignService campaignService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CampaignDto>>> GetAll(CancellationToken ct)
    {
        var campaigns = await db.Campaigns
            .Include(c => c.EmailTemplate)
            .Include(c => c.EmailList)
            .Include(c => c.SenderIdentity)
            .Include(c => c.Recipients)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => ToDto(c))
            .ToListAsync(ct);

        return campaigns;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CampaignDto>> GetById(Guid id, CancellationToken ct)
    {
        var campaign = await db.Campaigns
            .Include(c => c.EmailTemplate)
            .Include(c => c.EmailList)
            .Include(c => c.SenderIdentity)
            .Include(c => c.Recipients)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (campaign is null) return NotFound();
        return ToDto(campaign);
    }

    [HttpPost]
    public async Task<ActionResult<CampaignDto>> Create(CreateCampaignDto dto, CancellationToken ct)
    {
        var templateExists = await db.EmailTemplates.AnyAsync(t => t.Id == dto.EmailTemplateId, ct);
        if (!templateExists) return BadRequest("Email template not found.");

        if (dto.EmailListId.HasValue)
        {
            var listExists = await db.EmailLists.AnyAsync(l => l.Id == dto.EmailListId, ct);
            if (!listExists) return BadRequest("Email list not found.");
        }

        if (dto.SenderIdentityId.HasValue)
        {
            var senderOk = await db.SenderIdentities.AnyAsync(
                s => s.Id == dto.SenderIdentityId && s.IsActive, ct);
            if (!senderOk) return BadRequest("Sender identity not found or inactive.");
        }

        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            EmailTemplateId = dto.EmailTemplateId,
            EmailListId = dto.EmailListId,
            SenderIdentityId = dto.SenderIdentityId,
            SubjectOverride = dto.SubjectOverride?.Trim(),
            ScheduledAt = dto.ScheduledAt,
            Status = dto.ScheduledAt.HasValue ? CampaignStatus.Scheduled : CampaignStatus.Draft
        };

        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync(ct);

        await db.Entry(campaign).Reference(c => c.EmailTemplate).LoadAsync(ct);
        if (campaign.EmailListId.HasValue)
        {
            await db.Entry(campaign).Reference(c => c.EmailList).LoadAsync(ct);
        }

        if (campaign.SenderIdentityId.HasValue)
        {
            await db.Entry(campaign).Reference(c => c.SenderIdentity).LoadAsync(ct);
        }

        return CreatedAtAction(nameof(GetById), new { id = campaign.Id }, ToDto(campaign));
    }

    [HttpPost("{id:guid}/send")]
    public async Task<ActionResult<CampaignSendResultDto>> Send(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await campaignService.SendCampaignAsync(id, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var campaign = await db.Campaigns.FindAsync([id], ct);
        if (campaign is null) return NotFound();

        if (campaign.Status == CampaignStatus.Sending)
        {
            return Conflict("Cannot delete a campaign that is currently sending.");
        }

        db.Campaigns.Remove(campaign);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static CampaignDto ToDto(Campaign c) => new(
        c.Id,
        c.Name,
        c.EmailTemplateId,
        c.EmailTemplate.Name,
        c.EmailListId,
        c.EmailList?.Name,
        c.SenderIdentityId,
        c.SenderIdentity?.Email,
        c.SenderIdentity?.DisplayName,
        c.Status.ToString(),
        c.SubjectOverride,
        c.ScheduledAt,
        c.SentAt,
        c.CreatedAt,
        c.Recipients.Count,
        c.Recipients.Count(r => r.Status == RecipientStatus.Sent),
        c.Recipients.Count(r => r.Status == RecipientStatus.Failed));
}
