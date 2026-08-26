using LexVersusEmailMarketing.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace LexVersusEmailMarketing.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<EmailList> EmailLists => Set<EmailList>();
    public DbSet<EmailListContact> EmailListContacts => Set<EmailListContact>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignRecipient> CampaignRecipients => Set<CampaignRecipient>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<SenderIdentity> SenderIdentities => Set<SenderIdentity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(320).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.UnsubscribeToken).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.UnsubscribeToken).IsUnique();
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(320).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<EmailList>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<EmailListContact>(entity =>
        {
            entity.HasKey(e => new { e.EmailListId, e.ContactId });
            entity.HasOne(e => e.EmailList).WithMany(l => l.EmailListContacts).HasForeignKey(e => e.EmailListId);
            entity.HasOne(e => e.Contact).WithMany(c => c.EmailListContacts).HasForeignKey(e => e.ContactId);
        });

        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Subject).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<SenderIdentity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(320).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ReplyToEmail).HasMaxLength(320);
        });

        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.SubjectOverride).HasMaxLength(500);
            entity.HasOne(e => e.EmailTemplate).WithMany(t => t.Campaigns).HasForeignKey(e => e.EmailTemplateId);
            entity.HasOne(e => e.EmailList).WithMany().HasForeignKey(e => e.EmailListId);
            entity.HasOne(e => e.SenderIdentity).WithMany(s => s.Campaigns).HasForeignKey(e => e.SenderIdentityId);
        });

        modelBuilder.Entity<CampaignRecipient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Campaign).WithMany(c => c.Recipients).HasForeignKey(e => e.CampaignId);
            entity.HasOne(e => e.Contact).WithMany(c => c.CampaignRecipients).HasForeignKey(e => e.ContactId);
            entity.HasIndex(e => new { e.CampaignId, e.ContactId }).IsUnique();
        });
    }
}
