using GesDelta.Agents;
using GesDelta.Core.Enums;
using GesDelta.Core.Interfaces;
using GesDelta.Core.Models;
using GesDelta.Core.Options;
using GesDelta.Tests.Support;
using GesDelta.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GesDelta.Tests;

public class CollectParsePipelineTests
{
    private const string Config = """{"maxItems":100,"query":"neurosymbolic"}""";

    private static async Task<(FetchResult Fetch, List<(FetchedItem Item, ParseResult Result)> Parsed)>
        RunAsync(string fileName, SourceType type)
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Data", fileName);
        Assert.That(File.Exists(path), Is.True, $"Файл {fileName} не найден");

        var http = new HttpClient(new FileResponseHandler(path));

        ISourceFetcher? GetFetcher(SourceType t) => t switch
        {
            SourceType.Rss => new RssSourceFetcher(http),
            SourceType.Arxiv => new ArxivSourceFetcher(http),
            _ => null
        };

        var collector = new Collector(
            GetFetcher,
            Options.Create(new SourcesOptions()),
            NullLogger<Collector>.Instance);

        var source = new Source
        {
            Id = 1,
            Name = "test",
            Type = type,
            Url = type == SourceType.Arxiv
                ? "https://export.arxiv.org/api/query"
                : "https://example.com/feed",
            Config = Config
        };

        var fetch = await collector.CollectAsync(source, CancellationToken.None);

        var parser = new Parser();
        var parsed = fetch.Items
            .Select(item => (item, parser.Parse(item, SourceType.Html)))
            .ToList();

        return (fetch, parsed);
    }

    [TestCase("habr_feed.xml", SourceType.Rss)]
    [TestCase("arxiv_neurosymbolic.xml", SourceType.Arxiv)]
    public async Task Pipeline_FetchSucceeds_AndReturnsItems(string file, SourceType type)
    {
        var (fetch, _) = await RunAsync(file, type);

        Assert.That(fetch.Ok, Is.True, $"Collector вернул отказ: {fetch.Reason}");
        Assert.That(fetch.Items, Is.Not.Empty);
    }

    [TestCase("habr_feed.xml", SourceType.Rss)]
    [TestCase("arxiv_neurosymbolic.xml", SourceType.Arxiv)]
    public async Task Pipeline_EveryItemParses_WithNonEmptyText(string file, SourceType type)
    {
        var (_, parsed) = await RunAsync(file, type);

        var failed = parsed
            .Where(p => !p.Result.Ok)
            .Select(p => $"{p.Item.Title} ({p.Result.Reason})")
            .ToList();

        Assert.That(failed, Is.Empty,
            "Не разобраны записи:\n" + string.Join("\n", failed));

        Assert.That(parsed.Select(p => p.Result.Item!.CleanText),
            Has.All.Matches<string>(t => t.Trim().Length > 0));
    }

    [TestCase("habr_feed.xml", SourceType.Rss)]
    [TestCase("arxiv_neurosymbolic.xml", SourceType.Arxiv)]
    public async Task Pipeline_ExternalIds_AreUnique(string file, SourceType type)
    {
        var (fetch, _) = await RunAsync(file, type);

        var ids = fetch.Items.Select(i => i.ExternalId).ToList();

        Assert.That(ids, Is.Unique);
        Assert.That(ids, Has.None.Empty);
    }

    [TestCase("habr_feed.xml", SourceType.Rss)]
    [TestCase("arxiv_neurosymbolic.xml", SourceType.Arxiv)]
    public async Task Pipeline_ContentHashes_AreUnique(string file, SourceType type)
    {
        var (_, parsed) = await RunAsync(file, type);

        var hashes = parsed.Select(p => p.Result.Item!.ContentHash).ToList();

        Assert.That(hashes, Is.Unique,
            "Одинаковые хэши у разных записей: нормализатор выбрасывает слишком много?");
    }

    [TestCase("habr_feed.xml", SourceType.Rss)]
    [TestCase("arxiv_neurosymbolic.xml", SourceType.Arxiv)]
    public async Task Pipeline_Urls_AreAbsoluteHttps(string file, SourceType type)
    {
        var (_, parsed) = await RunAsync(file, type);

        Assert.That(parsed.Select(p => p.Result.Item!.Url),
            Has.All.StartsWith("http"));
    }

    [TestCase("habr_feed.xml", SourceType.Rss)]
    [TestCase("arxiv_neurosymbolic.xml", SourceType.Arxiv)]
    public async Task Pipeline_ReadMore_NeverSurvives(string file, SourceType type)
    {
        var (_, parsed) = await RunAsync(file, type);

        Assert.That(parsed.Select(p => p.Result.Item!.CleanText),
            Has.None.Contain("Читать далее"));
    }

    [TestCase("habr_feed.xml", SourceType.Rss)]
    [TestCase("arxiv_neurosymbolic.xml", SourceType.Arxiv)]
    public async Task Pipeline_IsDeterministic_AcrossTwoRuns(string file, SourceType type)
    {
        var first = await RunAsync(file, type);
        var second = await RunAsync(file, type);

        var h1 = first.Parsed.Select(p => p.Result.Item!.ContentHash).ToList();
        var h2 = second.Parsed.Select(p => p.Result.Item!.ContentHash).ToList();

        Assert.That(h2, Is.EqualTo(h1));
    }

    [Test]
    public async Task Arxiv_TitleAndAbstract_AreSeparateParagraphs()
    {
        var (_, parsed) = await RunAsync("arxiv_neurosymbolic.xml", SourceType.Arxiv);

        Assert.That(parsed.Select(p => p.Result.Item!.CleanText),
            Has.All.Contain("\n\n"));
    }

    [Test]
    public async Task Arxiv_AbstractLineWraps_AreJoinedIntoSentences()
    {
        var (_, parsed) = await RunAsync("arxiv_neurosymbolic.xml", SourceType.Arxiv);

        foreach (var (item, result) in parsed)
        {
            var paragraphs = result.Item!.CleanText.Split("\n\n");
            Assert.That(paragraphs, Has.All.Not.Contain("\n"),
                $"Перенос внутри абзаца: {item.Title}");
        }
    }

    [Test]
    public async Task Diagnostic_PrintFirstItems()
    {
        foreach (var (file, type) in new[]
                 {
                     ("habr_feed.xml", SourceType.Rss),
                     ("arxiv_neurosymbolic.xml", SourceType.Arxiv)
                 })
        {
            var (_, parsed) = await RunAsync(file, type);
            TestContext.WriteLine($"=== {file}: {parsed.Count} записей ===");

            foreach (var (item, result) in parsed.Take(3))
            {
                var text = result.Item?.CleanText ?? "<FAILURE>";
                var preview = text.Length > 200 ? text[..200] : text;
                TestContext.WriteLine(
                    $"[{item.Title}] len={text.Length}\n{preview.Replace("\n", "⏎")}\n");
            }
        }
    }

    [TestCase("We show that x < y holds.", "x < y")]
    [TestCase("We show that a<b holds.", "a<b holds")]
    [TestCase("Bounds like O(n<m) & more.", "O(n<m) & more")]
    public void Parser_Arxiv_PlainTextWithAngleBrackets_KeepsContent(
        string abstractText, string mustContain)
    {
        var item = new FetchedItem(
            "1", "Title", "https://example.com", null,
            $"Title\n\n{abstractText}", FromPage: false);

        var result = new Parser().Parse(item, SourceType.Arxiv);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Item!.CleanText, Does.Contain(mustContain));
    }
    
    [Test]
    public void Parser_Rss_EscapedAngleBracket_IsDecoded()
    {
        var item = new FetchedItem(
            "1", "T", "https://example.com", null,
            "<p>Если a &lt; b, то всё хорошо</p>", FromPage: false);

        var result = new Parser().Parse(item, SourceType.Rss);

        Assert.That(result.Item!.CleanText, Is.EqualTo("Если a < b, то всё хорошо"));
    }
}