using GesDelta.Agents;
using GesDelta.Core.Interfaces;
using GesDelta.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using GesDelta.Core.Options;
using GesDelta.Tools;

namespace GesDelta.Orchestrator;

public static class MonitoringExtensions
{
    public static IHostApplicationBuilder AddMonitoring(this IHostApplicationBuilder builder)
    {
        string dir = Environment.ExpandEnvironmentVariables(builder.Configuration["Logging:Dir"] 
                                                             ?? throw new InvalidOperationException("Configuration key 'Logging:Dir' is not set"));
        
        var retainDays = builder.Configuration.GetValue<int>("Logging:RetainDays");
        if (retainDays < 1)
            throw new InvalidOperationException("Configuration key 'Logging:RetainDays' must be >= 1");
        
        var loggerConfig = new LoggerConfiguration()
            .WriteTo.File(Path.Combine(dir,"gesdelta-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: retainDays);

        builder.Services.AddSerilog(loggerConfig.CreateLogger(), dispose:true);
        
        var connectionString = builder.Configuration.GetConnectionString("Default")
                               ?? throw new InvalidOperationException("Connection string is not found ConnectionStrings:Default");

        builder.Services.AddStorage(connectionString);
        
        builder.Services.AddSourceFetching(builder.Configuration);
        
        builder.Services.AddHostedService<StartupHostedService>(); //Планировщик регистрируется ниже
        
        builder.Services.Configure<DetectorOptions>(
            builder.Configuration.GetSection("Detector"));

        builder.Services.AddScoped<Collector>();
        builder.Services.AddScoped<IMonitoringService, MonitoringService>();
        builder.Services.AddScoped<CollectRunner>();
        builder.Services.AddScoped<ITextDiffer, DiffPlexTextDiffer>();
        builder.Services.AddSingleton<ITextSimilarity, TextSimilarity>();
        builder.Services.AddScoped<ISignificanceJudge, SignificanceJudge>();
        builder.Services.AddScoped<Detector>();
        builder.Services.AddSingleton<IParser, Parser>();
        builder.Services.AddSingleton(TimeProvider.System);

        return builder;
    }
}