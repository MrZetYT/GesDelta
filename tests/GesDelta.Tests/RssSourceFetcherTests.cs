using GesDelta.Core.Enums;
using GesDelta.Core.Models;
using GesDelta.Tools;

namespace GesDelta.Tests;

public class RssSourceFetcherTests
{
    private static async Task<FetchResult> FetchAsync(HttpClient httpClient, int maxItems = 40)
    {
        var rssSourceFetcher = new RssSourceFetcher(httpClient);
        
        return await rssSourceFetcher.FetchAsync(
            new FetchRequest(
                1,
                SourceType.Rss,
                "https://example.com/feed",
                "",
                maxItems), CancellationToken.None);;
    }
    
    [Test]
    public async Task Success_ResultOk()
    {
        var file = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Data", "habr_feed.xml"));

        var httpClient = new HttpClient(new FakeHttpMessageHandler(file));

        var result = await FetchAsync(httpClient);
        
        Assert.That(result.Ok, Is.True);
        Assert.That(result.Items.Count, Is.GreaterThan(0));
        foreach (var item in result.Items)
        {
            Assert.That(string.IsNullOrWhiteSpace(item.ExternalId), Is.False);
            Assert.That(string.IsNullOrWhiteSpace(item.Title), Is.False);
            Assert.That(string.IsNullOrWhiteSpace(item.Url), Is.False);
            Assert.That(item.FromPage, Is.False);
        }
    }
    
    [Test]
    public async Task Success_ResultOk_With5Items()
    {
        var file = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Data", "habr_feed.xml"));

        var httpClient = new HttpClient(new FakeHttpMessageHandler(file));
        
        var result = await FetchAsync(httpClient, 5);
        
        Assert.That(result.Ok, Is.True);
        Assert.That(result.Items.Count, Is.EqualTo(5));
        foreach (var item in result.Items)
        {
            Assert.That(string.IsNullOrWhiteSpace(item.ExternalId), Is.False);
            Assert.That(string.IsNullOrWhiteSpace(item.Title), Is.False);
            Assert.That(string.IsNullOrWhiteSpace(item.Url), Is.False);
            Assert.That(item.FromPage, Is.False);
        }
    }
    
    [Test]
    public async Task Fetch_NotAFeed_ReturnsFormatChanged()
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler("<html>not a feed</html>"));

        var result = await FetchAsync(httpClient);
        
        Assert.That(result.Ok, Is.False);
        Assert.That(result.Reason, Is.EqualTo(FetchFailureReason.FormatChanged));
    }
}