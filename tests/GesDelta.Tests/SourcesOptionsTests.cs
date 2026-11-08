using GesDelta.Core.Options;

namespace GesDelta.Tests;

public class SourcesOptionsTests
{
    private static SourcesOptions CreateOptions() => new()
    {
        HostDelayMs = 1000,
        HostDelaysMs =
        {
            ["habr.com"] = 10000,
            ["export.arxiv.org"] = 3000
        }
    };

    [TestCase("habr.com", 10000)]
    [TestCase("export.arxiv.org", 3000)]
    public void GetDelayMs_HostInDictionary_ReturnsHostDelay(string host, int expectedMs)
    {
        var options = CreateOptions();

        Assert.That(options.GetDelayMs(host), Is.EqualTo(expectedMs));
    }

    [TestCase("example.com")]
    [TestCase("")]
    public void GetDelayMs_UnknownHost_ReturnsDefaultDelay(string host)
    {
        var options = CreateOptions();

        Assert.That(options.GetDelayMs(host), Is.EqualTo(1000));
    }

    [Test]
    public void GetDelayMs_EmptyDictionary_ReturnsDefaultDelay()
    {
        var options = new SourcesOptions { HostDelayMs = 2500 };

        Assert.That(options.GetDelayMs("habr.com"), Is.EqualTo(2500));
    }
}