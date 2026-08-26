using LexVersusEmailMarketing.Core.Interfaces;
using LexVersusEmailMarketing.Infrastructure.Data;
using LexVersusEmailMarketing.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LexVersusEmailMarketing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IContactImportService, ContactImportService>();
        services.AddScoped<IEmailSenderService, SendGridEmailService>();
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IUnsubscribeService, UnsubscribeService>();
        services.AddHostedService<CampaignSchedulerHostedService>();

        return services;
    }
}
