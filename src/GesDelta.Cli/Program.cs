using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var appSettings = new HostApplicationBuilderSettings()
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
};

var builder = Host.CreateApplicationBuilder(appSettings);

var retainDays = builder.Configuration["Logging:RetainDays"];

Console.WriteLine(retainDays);