using LexVersusEmailMarketing.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LexVersusEmailMarketing.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await db.Database.EnsureCreatedAsync();
        await ApplySchemaPatchesAsync(db);
        await BackfillUnsubscribeTokensAsync(db);
        await SeedSendersAsync(db, configuration);
    }

    private static async Task ApplySchemaPatchesAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[dbo].[SenderIdentities]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[SenderIdentities] (
                    [Id] uniqueidentifier NOT NULL,
                    [Email] nvarchar(320) NOT NULL,
                    [DisplayName] nvarchar(200) NOT NULL,
                    [ReplyToEmail] nvarchar(320) NULL,
                    [IsDefault] bit NOT NULL,
                    [IsActive] bit NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_SenderIdentities] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_SenderIdentities_Email] ON [dbo].[SenderIdentities] ([Email]);
            END
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('SenderIdentities', 'ReplyToEmail') IS NULL
            BEGIN
                ALTER TABLE [dbo].[SenderIdentities] ADD [ReplyToEmail] nvarchar(320) NULL;
            END
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('Campaigns', 'SenderIdentityId') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Campaigns] ADD [SenderIdentityId] uniqueidentifier NULL;
            END
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (
                SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Campaigns_SenderIdentities_SenderIdentityId'
            )
            AND COL_LENGTH('Campaigns', 'SenderIdentityId') IS NOT NULL
            AND OBJECT_ID(N'[dbo].[SenderIdentities]', N'U') IS NOT NULL
            BEGIN
                ALTER TABLE [dbo].[Campaigns]
                ADD CONSTRAINT [FK_Campaigns_SenderIdentities_SenderIdentityId]
                FOREIGN KEY ([SenderIdentityId]) REFERENCES [dbo].[SenderIdentities] ([Id]);
            END
            """);
    }

    private static async Task BackfillUnsubscribeTokensAsync(AppDbContext db)
    {
        var contacts = await db.Contacts
            .Where(c => c.UnsubscribeToken == null || c.UnsubscribeToken == "")
            .ToListAsync();

        foreach (var contact in contacts)
        {
            contact.UnsubscribeToken = Guid.NewGuid().ToString("N");
        }

        if (contacts.Count > 0)
        {
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedSendersAsync(AppDbContext db, IConfiguration configuration)
    {
        var configured = new[]
        {
            new { Email = "aquagen.info@versus-solutions.hr", DisplayName = "AquaGen Versus Solutions", ReplyTo = "aquageninforeplay@versus-solutions.hr", IsDefault = false },
            new { Email = "usluge@naplata-potrazivanja.hr", DisplayName = "Naplata potraživanja", ReplyTo = "uslugereplay@napata-potrazivanja.hr", IsDefault = false },
            new { Email = "afm@lexversus.org", DisplayName = "AFM LexVersus", ReplyTo = "afmreply@lexversus.org", IsDefault = true }
        };

        var existing = await db.SenderIdentities.ToListAsync();
        var changed = false;

        foreach (var item in configured)
        {
            var email = item.Email.ToLowerInvariant();
            var sender = existing.FirstOrDefault(s => s.Email == email);
            if (sender is null)
            {
                db.SenderIdentities.Add(new SenderIdentity
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    DisplayName = item.DisplayName,
                    ReplyToEmail = item.ReplyTo.ToLowerInvariant(),
                    IsDefault = item.IsDefault,
                    IsActive = true
                });
                changed = true;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(sender.ReplyToEmail))
                {
                    sender.ReplyToEmail = item.ReplyTo.ToLowerInvariant();
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(sender.DisplayName))
                {
                    sender.DisplayName = item.DisplayName;
                    changed = true;
                }
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync();

            var all = await db.SenderIdentities.ToListAsync();
            if (!all.Any(s => s.IsDefault) && all.Count > 0)
            {
                foreach (var s in all) s.IsDefault = false;
                var afm = all.FirstOrDefault(s => s.Email == "afm@lexversus.org") ?? all[0];
                afm.IsDefault = true;
                await db.SaveChangesAsync();
            }
            else if (all.Count(s => s.IsDefault) > 1)
            {
                var keep = all.FirstOrDefault(s => s.Email == "afm@lexversus.org" && s.IsDefault)
                    ?? all.First(s => s.IsDefault);
                foreach (var s in all.Where(x => x.Id != keep.Id))
                {
                    s.IsDefault = false;
                }
                await db.SaveChangesAsync();
            }
        }

        // Fallback from config if still empty
        if (!await db.SenderIdentities.AnyAsync())
        {
            var email = configuration["SendGrid:FromEmail"];
            var name = configuration["SendGrid:FromName"] ?? "LexVersus Marketing";
            if (!string.IsNullOrWhiteSpace(email) && email != "marketing@yourdomain.com")
            {
                db.SenderIdentities.Add(new SenderIdentity
                {
                    Id = Guid.NewGuid(),
                    Email = email.Trim().ToLowerInvariant(),
                    DisplayName = name.Trim(),
                    IsDefault = true,
                    IsActive = true
                });
                await db.SaveChangesAsync();
            }
        }
    }
}
