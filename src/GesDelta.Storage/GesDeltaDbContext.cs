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
    public DbSet<Change> Changes => Set<Change>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<ClaimEvidence> ClaimEvidences => Set<ClaimEvidence>();
    
    public GesDeltaDbContext(DbContextOptions<GesDeltaDbContext> options)
        : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GesDeltaDbContext).Assembly);
        modelBuilder.HasSequence<long>("claims_id_seq");
        base.OnModelCreating(modelBuilder);
    }
}