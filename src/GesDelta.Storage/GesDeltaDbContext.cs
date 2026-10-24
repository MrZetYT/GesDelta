using Microsoft.EntityFrameworkCore;

namespace GesDelta.Storage;

public class GesDeltaDbContext : DbContext
{
    public GesDeltaDbContext(DbContextOptions<GesDeltaDbContext> options)
        : base(options)
    {
    }
}