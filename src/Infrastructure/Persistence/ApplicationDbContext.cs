using Microsoft.EntityFrameworkCore;
using WeeklyUp.Domain.Entities;

namespace WeeklyUp.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Integration> Integrations => Set<Integration>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportPreference> ReportPreferences => Set<ReportPreference>();
    public DbSet<ManualMetric> ManualMetrics => Set<ManualMetric>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
