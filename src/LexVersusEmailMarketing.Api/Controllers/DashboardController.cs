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
public class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;

        var stats = new DashboardStatsDto(
            await db.Contacts.CountAsync(ct),
            await db.EmailTemplates.CountAsync(ct),
            await db.Campaigns.CountAsync(ct),
            await db.Campaigns.CountAsync(c => c.Status == CampaignStatus.Sending || c.Status == CampaignStatus.Scheduled, ct),
            await db.CampaignRecipients.CountAsync(r => r.SentAt != null && r.SentAt >= today, ct));

        return stats;
    }
}
