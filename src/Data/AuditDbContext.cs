using Microsoft.EntityFrameworkCore;
using NotificationAuditService.Audit;
using NotificationAuditService.Expenses;
using NotificationAuditService.Notifications;
using NotificationAuditService.Projects;
using NotificationAuditService.Transactions;

namespace NotificationAuditService.Data;

public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
    {
    }

    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<SharedExpense> SharedExpenses { get; set; }
    public DbSet<ExpenseParticipant> ExpenseParticipants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.UserName).HasMaxLength(255);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.HasIndex(e => e.EntityName);
            entity.HasIndex(e => new { e.EntityName, e.EntityId });
            entity.HasIndex(e => e.Timestamp);
        });

        // Notification configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RecipientId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RecipientEmail).HasMaxLength(255);
            entity.Property(e => e.RelatedEntityType).HasMaxLength(255);
            entity.HasIndex(e => e.RecipientId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.RecipientId, e.IsRead });
        });

        // Project configuration
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrganizationId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.TeamId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);

            // Tenant-first composite indexes: every access pattern begins with the organisation,
            // matching how the repository scopes each query.
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => new { e.OrganizationId, e.TeamId });
            entity.HasIndex(e => new { e.OrganizationId, e.TeamId, e.Status });
            entity.HasIndex(e => new { e.OrganizationId, e.CreatedAt });
        });

        // Transaction configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrganizationId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Description).HasMaxLength(1000);

            // Tenant-first composite indexes: every access pattern begins with the organisation,
            // matching how the repository scopes each query.
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => new { e.OrganizationId, e.UserId });
            entity.HasIndex(e => new { e.OrganizationId, e.UserId, e.CreatedAt });
        });

        // SharedExpense configuration
        modelBuilder.Entity<SharedExpense>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrganizationId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.GroupId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(10);
            entity.Property(e => e.PaidByUserId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.SplitType).HasConversion<string>().HasMaxLength(20);

            // Tenant-first composite indexes.
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => new { e.OrganizationId, e.GroupId });
            entity.HasIndex(e => new { e.OrganizationId, e.GroupId, e.CreatedAt });

            entity.HasMany(e => e.Participants)
                  .WithOne()
                  .HasForeignKey(p => p.SharedExpenseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ExpenseParticipant configuration
        modelBuilder.Entity<ExpenseParticipant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ShareAmount).HasPrecision(18, 2);
            entity.HasIndex(e => e.SharedExpenseId);
        });
    }
}