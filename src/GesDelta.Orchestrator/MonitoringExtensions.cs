using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;

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

        return builder;
    }
}