using LexVersusEmailMarketing.Core.DTOs;
using LexVersusEmailMarketing.Core.Interfaces;
using LexVersusEmailMarketing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LexVersusEmailMarketing.Infrastructure.Services;

public class UnsubscribeService(AppDbContext db) : IUnsubscribeService
{
    public async Task<UnsubscribeInfoDto?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var contact = await db.Contacts
            .FirstOrDefaultAsync(c => c.UnsubscribeToken == token, cancellationToken);

        if (contact is null) return null;

        return new UnsubscribeInfoDto(contact.Email, contact.IsSubscribed, contact.UnsubscribedAt);
    }

    public async Task<UnsubscribeResultDto> UnsubscribeAsync(string token, CancellationToken cancellationToken = default)
    {
        var contact = await db.Contacts
            .FirstOrDefaultAsync(c => c.UnsubscribeToken == token, cancellationToken);

        if (contact is null)
        {
            return new UnsubscribeResultDto(false, "Invalid or expired unsubscribe link.");
        }

        if (!contact.IsSubscribed)
        {
            return new UnsubscribeResultDto(true, "You are already unsubscribed from our mailing list.");
        }

        contact.IsSubscribed = false;
        contact.UnsubscribedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new UnsubscribeResultDto(true, "You have been successfully unsubscribed.");
    }
}
