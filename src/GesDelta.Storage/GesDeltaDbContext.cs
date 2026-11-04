using GesDelta.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GesDelta.Storage;

public class GesDeltaDbContext : DbContext
{
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Source> Sources => Set<Source>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Run> Runs => Set<Run>();
    public DbSet<Snapshot> Snapshots => Set<Snapshot>();
    
    public GesDeltaDbContext(DbContextOptions<GesDeltaDbContext> options)
        : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GesDeltaDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}