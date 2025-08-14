using ServiceControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ServiceControl.Infrastructure.Persistence.Configurations;

namespace ServiceControl.Infrastructure.Persistence.Contexts;

public class WorkRecordContext : DbContext
{
    public WorkRecordContext(DbContextOptions<WorkRecordContext> options) : base(options) { }
    public DbSet<WorkRecord> WorkRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new WorkRecordConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}