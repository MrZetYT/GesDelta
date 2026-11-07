using System.Net;
using GesDelta.Core.Enums;
using GesDelta.Core.Models;
using GesDelta.Tools;

namespace GesDelta.Tests;

public class ArxivSourceFetcherTests
{
    private const string RecordedFeedFile = "arxiv_neurosymbolic.xml";

    private const string ValidConfig = """{"query":"neurosymbolic"}""";

    private static Task<FetchResult> FetchAsync(
        HttpMessageHandler handler, string config = ValidConfig, int maxItems = 40)
    {
        var fetcher = new ArxivSourceFetcher(new HttpClient(handler));
        return fetcher.FetchAsync(
            new FetchRequest(1, SourceType.Arxiv, "https://export.arxiv.org/api/query", config, maxItems),
            CancellationToken.None);
    }

    private static async Task<string> ReadRecordedFeedAsync() =>
        await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Data", RecordedFeedFile));

    private static string Feed(string entries) =>
        $"""<feed xmlns="http://www.w3.org/2005/Atom">{entries}</feed>""";

    [Test]
    public async Task Fetch_RecordedFeed_ReturnsCleanItems()
    {
        var result = await FetchAsync(new FakeHttpMessageHandler(await ReadRecordedFeedAsync()));

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Items.Count, Is.GreaterThan(0));
        foreach (var item in result.Items)
        {
            Assert.That(item.ExternalId, Is.Not.Empty);
            Assert.That(item.ExternalId, Does.Not.Contain("/abs/"));
            Assert.That(item.ExternalId, Does.Not.Match(@"v\d+$"));
            Assert.That(string.IsNullOrWhiteSpace(item.Title), Is.False);
            Assert.That(string.IsNullOrWhiteSpace(item.Url), Is.False);
            Assert.That(item.PublishedAt, Is.Not.Null);
            Assert.That(item.FromPage, Is.False);
        }
    }

    [Test]
    public async Task Fetch_MaxItems3_ReturnsExactly3()
    {
        var result = await FetchAsync(new FakeHttpMessageHandler(await ReadRecordedFeedAsync()), maxItems: 3);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Items.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task Fetch_OldStyleIdWithSlash_KeepsSlashAndDropsVersion()
    {
        var xml = Feed("""
            <entry>
              <id>http://arxiv.org/abs/hep-th/9901001v1</id>
              <title>Old paper</title>
              <summary>Some abstract</summary>
              <published>1999-01-01T00:00:00Z</published>
              <link href="http://arxiv.org/abs/hep-th/9901001v1" rel="alternate" type="text/html"/>
            </entry>
            """);

        var result = await FetchAsync(new FakeHttpMessageHandler(xml));

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Items.Count, Is.EqualTo(1));
        Assert.That(result.Items[0].ExternalId, Is.EqualTo("hep-th/9901001"));
        Assert.That(result.Items[0].RawText, Is.EqualTo("Old paper\n\nSome abstract"));
    }

    [Test]
    public async Task Fetch_EntryWithoutAbsInId_IsSkipped()
    {
        var xml = Feed("""
            <entry>
              <id>http://arxiv.org/api/errors#incorrect_id_format</id>
              <title>Error</title>
              <summary>something went wrong</summary>
            </entry>
            """);

        var result = await FetchAsync(new FakeHttpMessageHandler(xml));

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Items, Is.Empty);
    }

    [Test]
    public async Task Fetch_EmptyFeed_ReturnsOkWithNoItems()
    {
        var result = await FetchAsync(new FakeHttpMessageHandler(Feed("")));

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Items, Is.Empty);
    }

    [TestCase("")]
    [TestCase("{")]
    [TestCase("{}")]
    [TestCase("null")]
    [TestCase("""{"query":"   "}""")]
    public async Task Fetch_BadConfig_ReturnsInvalidConfig(string config)
    {
        var result = await FetchAsync(new FakeHttpMessageHandler(Feed("")), config);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Reason, Is.EqualTo(FetchFailureReason.InvalidConfig));
    }

    [Test]
    public async Task Fetch_MalformedXml_ReturnsFormatChanged()
    {
        var result = await FetchAsync(new FakeHttpMessageHandler("this is not xml <"));

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Reason, Is.EqualTo(FetchFailureReason.FormatChanged));
    }

    [Test]
    public async Task Fetch_Http404_ReturnsHttp404()
    {
        var result = await FetchAsync(new FakeHttpMessageHandler("", HttpStatusCode.NotFound));

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Reason, Is.EqualTo(FetchFailureReason.Http404));
    }

    [Test]
    public async Task Fetch_BuildsRequestUrlFromConfig()
    {
        var handler = new FakeHttpMessageHandler(Feed(""));

        await FetchAsync(handler, """{"query":"knowledge graph"}""", maxItems: 7);

        var query = handler.LastRequestUri!.Query;
        Assert.That(query, Does.Contain("search_query=all:knowledge%20graph"));
        Assert.That(query, Does.Contain("max_results=7"));
        Assert.That(query, Does.Contain("sortBy=submittedDate"));
        Assert.That(query, Does.Contain("sortOrder=descending"));
    }
}