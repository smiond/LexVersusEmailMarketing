using LexVersusEmailMarketing.Core.Entities;
using LexVersusEmailMarketing.Core.Interfaces;
using LexVersusEmailMarketing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LexVersusEmailMarketing.Infrastructure.Services;

public class CampaignSchedulerHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<CampaignSchedulerHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueCampaignsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error processing scheduled campaigns.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessDueCampaignsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();

        var now = DateTime.UtcNow;
        var dueCampaignIds = await db.Campaigns
            .Where(c => c.Status == CampaignStatus.Scheduled && c.ScheduledAt != null && c.ScheduledAt <= now)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        foreach (var campaignId in dueCampaignIds)
        {
            try
            {
                logger.LogInformation("Auto-sending scheduled campaign {CampaignId}", campaignId);
                await campaignService.SendCampaignAsync(campaignId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to auto-send campaign {CampaignId}", campaignId);
            }
        }
    }
}
