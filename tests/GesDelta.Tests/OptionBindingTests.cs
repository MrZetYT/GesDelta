using GesDelta.Core.Options;
using Microsoft.Extensions.Configuration;

namespace GesDelta.Tests;

public class OptionsBindingTests
{
    private static IConfiguration LoadCliAppSettings() =>
        new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "cli-appsettings.json"), optional: false)
            .Build();

    [Test]
    public void CliAppSettings_CollectorSection_BindsPauses()
    {
        var options = new CollectorOptions();

        LoadCliAppSettings().GetSection("Orchestration:Collector").Bind(options);

        Assert.That(options.PausesSec, Is.EqualTo(new[] { 2, 5, 10 }));
    }

    [Test]
    public void CliAppSettings_SourcesSection_BindsValuesAndHostDelays()
    {
        var options = new SourcesOptions();

        LoadCliAppSettings().GetSection("Sources").Bind(options);

        Assert.That(options.HostDelayMs, Is.EqualTo(1000));
        Assert.That(options.RssFullTextMinChars, Is.EqualTo(1500));
        Assert.That(options.MaxNewMaterialsPerRun, Is.EqualTo(10));
        Assert.That(options.MaxItemsPerSource, Is.EqualTo(20));
        Assert.That(options.GetDelayMs("habr.com"), Is.EqualTo(10000));
        Assert.That(options.GetDelayMs("export.arxiv.org"), Is.EqualTo(3000));
        Assert.That(options.GetDelayMs("example.com"), Is.EqualTo(1000));
    }

    [Test]
    public void EmptyConfig_CollectorOptions_HasNoPauses()
    {
        var options = new CollectorOptions();
        var emptyConfig = new ConfigurationBuilder().Build();

        emptyConfig.GetSection("Orchestration:Collector").Bind(options);

        Assert.That(options.PausesSec, Is.Empty);
    }
}