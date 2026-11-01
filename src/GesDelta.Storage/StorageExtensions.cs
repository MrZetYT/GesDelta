using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GesDelta.Storage;

public static class StorageExtensions
{
    public static IServiceCollection AddStorage(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<GesDeltaDbContext>(
            options => options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());
        return services;
    }
}