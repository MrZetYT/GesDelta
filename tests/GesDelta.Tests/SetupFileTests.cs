using System.Text.Json;
using GesDelta.Cli;
using GesDelta.Core.Enums;

namespace GesDelta.Tests;

public class SetupFileTests
{
    private const string Valid = """
    {
      "topic": "гибридные интеллектуальные системы",
      "checkIntervalDays": 7,
      "sources": [
        { "name": "habr-ml", "type": "Rss",
          "url": "https://habr.com/ru/rss/hubs/machine_learning/articles/?fl=ru",
          "config": { "fullText": "auto" } },
        { "name": "arxiv", "type": "Arxiv",
          "url": "https://export.arxiv.org/api/query",
          "config": { "query": "neurosymbolic", "maxItems": 10 } }
      ]
    }
    """;

    [Test]
    public void Parse_ValidFile_ReturnsTopicAndInterval()
    {
        var request = SetupFile.Parse(Valid);

        Assert.That(request.TopicName, Is.EqualTo("гибридные интеллектуальные системы"));
        Assert.That(request.CheckInterval, Is.EqualTo(TimeSpan.FromDays(7)));
    }

    [Test]
    public void Parse_ValidFile_ReturnsSourcesInOrder()
    {
        var request = SetupFile.Parse(Valid);

        Assert.That(request.Sources, Has.Count.EqualTo(2));
        Assert.That(request.Sources.Select(s => s.Name), Is.EqualTo(new[] { "habr-ml", "arxiv" }));
        Assert.That(request.Sources.Select(s => s.Type),
            Is.EqualTo(new[] { SourceType.Rss, SourceType.Arxiv }));
        Assert.That(request.Sources[1].Url, Is.EqualTo("https://export.arxiv.org/api/query"));
    }

    [Test]
    public void Parse_ConfigObject_BecomesJsonString()
    {
        var request = SetupFile.Parse(Valid);

        using var doc = JsonDocument.Parse(request.Sources[1].ConfigJson!);
        Assert.That(doc.RootElement.GetProperty("query").GetString(), Is.EqualTo("neurosymbolic"));
        Assert.That(doc.RootElement.GetProperty("maxItems").GetInt32(), Is.EqualTo(10));
    }

    [Test]
    public void Parse_NoConfig_GivesNullConfigJson()
    {
        const string json = """
        { "topic": "t", "checkIntervalDays": 7,
          "sources": [ { "name": "a", "type": "Html", "url": "https://example.com/news" } ] }
        """;

        var request = SetupFile.Parse(json);

        Assert.That(request.Sources[0].ConfigJson, Is.Null);
    }

    [TestCase("rss")]
    [TestCase("RSS")]
    [TestCase("Rss")]
    public void Parse_TypeIsCaseInsensitive(string type)
    {
        var json = $$"""
        { "topic": "t", "checkIntervalDays": 7,
          "sources": [ { "name": "a", "type": "{{type}}", "url": "https://example.com/f" } ] }
        """;

        Assert.That(SetupFile.Parse(json).Sources[0].Type, Is.EqualTo(SourceType.Rss));
    }

    [Test]
    public void Parse_FractionalDays_Works()
    {
        const string json = """
        { "topic": "t", "checkIntervalDays": 0.5,
          "sources": [ { "name": "a", "type": "Rss", "url": "https://example.com/f" } ] }
        """;

        Assert.That(SetupFile.Parse(json).CheckInterval, Is.EqualTo(TimeSpan.FromHours(12)));
    }

    [Test]
    public void Parse_UnknownExtraFields_AreIgnored()
    {
        const string json = """
        { "topic": "t", "checkIntervalDays": 7, "comment": "заметка",
          "sources": [ { "name": "a", "type": "Rss", "url": "https://example.com/f", "note": 1 } ] }
        """;

        Assert.DoesNotThrow(() => SetupFile.Parse(json));
    }

    [TestCase("{ not json", TestName = "InvalidJson")]
    [TestCase("", TestName = "EmptyString")]
    [TestCase("[]", TestName = "RootIsArray")]
    [TestCase("null", TestName = "RootIsNull")]
    [TestCase("""{"checkIntervalDays":7,"sources":[{"name":"a","type":"Rss","url":"https://x.com/f"}]}""",
        TestName = "NoTopic")]
    [TestCase("""{"topic":"t","sources":[{"name":"a","type":"Rss","url":"https://x.com/f"}]}""",
        TestName = "NoInterval")]
    [TestCase("""{"topic":"t","checkIntervalDays":"seven","sources":[{"name":"a","type":"Rss","url":"https://x.com/f"}]}""",
        TestName = "IntervalIsString")]
    [TestCase("""{"topic":"t","checkIntervalDays":7}""", TestName = "NoSources")]
    [TestCase("""{"topic":"t","checkIntervalDays":7,"sources":{}}""", TestName = "SourcesIsObject")]
    [TestCase("""{"topic":5,"checkIntervalDays":7,"sources":[{"name":"a","type":"Rss","url":"https://x.com/f"}]}""",
        TestName = "TopicIsNumber")]
    [TestCase("""{"topic":"t","checkIntervalDays":7,"sources":[{"type":"Rss","url":"https://x.com/f"}]}""",
        TestName = "SourceWithoutName")]
    [TestCase("""{"topic":"t","checkIntervalDays":7,"sources":[{"name":"a","url":"https://x.com/f"}]}""",
        TestName = "SourceWithoutType")]
    [TestCase("""{"topic":"t","checkIntervalDays":7,"sources":[{"name":"a","type":"Rss"}]}""",
        TestName = "SourceWithoutUrl")]
    [TestCase("""{"topic":"t","checkIntervalDays":7,"sources":[{"name":"a","type":"Atom","url":"https://x.com/f"}]}""",
        TestName = "UnknownSourceType")]
    [TestCase("""{"topic":"t","checkIntervalDays":7,"sources":["https://x.com/f"]}""",
        TestName = "SourceIsString")]
    public void Parse_BadFile_ThrowsFormatException(string json)
    {
        Assert.Throws<FormatException>(() => SetupFile.Parse(json));
    }

    [Test]
    public void Parse_ErrorMessage_NamesTheSource()
    {
        const string json = """
        { "topic": "t", "checkIntervalDays": 7,
          "sources": [ { "name": "broken-one", "type": "Atom", "url": "https://x.com/f" } ] }
        """;

        var ex = Assert.Throws<FormatException>(() => SetupFile.Parse(json));

        Assert.That(ex!.Message, Does.Contain("Atom").Or.Contain("broken-one"));
    }
}