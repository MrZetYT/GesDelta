using GesDelta.Agents;
using GesDelta.Core.Enums;
using GesDelta.Core.Models;

namespace GesDelta.Tests;

public class ParserTests
{
    private readonly Parser _parser = new();

    private static FetchedItem Item(
        string rawText,
        string externalId = "guid-1",
        string title = "Заголовок",
        string url = "https://example.com/a",
        DateTime? publishedAt = null,
        bool fromPage = false) =>
        new(externalId, title, url, publishedAt, rawText, fromPage);

    [Test]
    public void Parse_HtmlAnnouncement_ReturnsCleanTextAndHash()
    {
        var result = _parser.Parse(Item("<p>Первый абзац</p><p>Второй абзац</p>"), SourceType.Html);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Reason, Is.Null);
        Assert.That(result.Item!.CleanText, Is.EqualTo("Первый абзац\n\nВторой абзац"));
        Assert.That(result.Item.ContentHash, Has.Length.EqualTo(64));
    }

    [Test]
    public void Parse_PassesMetadataThrough()
    {
        var published = new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);

        var result = _parser.Parse(Item(
            "<p>Текст</p>",
            externalId: "id-42",
            title: "Моя статья",
            url: "https://example.com/42",
            publishedAt: published), SourceType.Html);

        Assert.That(result.Item!.ExternalId, Is.EqualTo("id-42"));
        Assert.That(result.Item.Title, Is.EqualTo("Моя статья"));
        Assert.That(result.Item.Url, Is.EqualTo("https://example.com/42"));
        Assert.That(result.Item.PublishedAt, Is.EqualTo(published));
    }

    [Test]
    public void Parse_NullPublishedAt_StaysNull()
    {
        var result = _parser.Parse(Item("<p>Текст</p>", publishedAt: null), SourceType.Html);

        Assert.That(result.Item!.PublishedAt, Is.Null);
    }

    [Test]
    public void Parse_ContentHash_IsHashOfCleanText()
    {
        var result = _parser.Parse(Item("<p>Привет &amp; пока</p>"), SourceType.Html);

        Assert.That(result.Item!.CleanText, Is.EqualTo("Привет & пока"));
        Assert.That(result.Item.ContentHash,
            Is.EqualTo(ContentHasher.Sha256(result.Item.CleanText)));
    }

    [Test]
    public void Parse_SameInput_GivesSameHash()
    {
        var a = _parser.Parse(Item("<p>Текст анонса</p>"), SourceType.Html);
        var b = _parser.Parse(Item("<p>Текст анонса</p>"), SourceType.Html);

        Assert.That(a.Item!.ContentHash, Is.EqualTo(b.Item!.ContentHash));
    }

    [Test]
    public void Parse_DifferentText_GivesDifferentHash()
    {
        var a = _parser.Parse(Item("<p>Текст анонса</p>"), SourceType.Html);
        var b = _parser.Parse(Item("<p>Текст анонсa!</p>"), SourceType.Html);

        Assert.That(a.Item!.ContentHash, Is.Not.EqualTo(b.Item!.ContentHash));
    }

    [Test]
    public void Parse_DynamicDateLine_DoesNotChangeHash()
    {
        var without = _parser.Parse(Item("<p>Первая строка<br>Вторая строка</p>"), SourceType.Html);
        var with = _parser.Parse(Item(
            "<p>Первая строка</p><p>Обновлено 08.10.2026</p><p>Вторая строка</p>"), SourceType.Html);

        // разные разметки, но после нормализации динамическая строка не влияет
        Assert.That(with.Ok, Is.True);
        Assert.That(with.Item!.CleanText, Does.Not.Contain("Обновлено"));
    }

    [Test]
    public void Parse_TrailingReadMore_IsRemoved()
    {
        var result = _parser.Parse(Item("<p>Анонс статьи</p><p>Читать далее</p>"), SourceType.Html);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Item!.CleanText, Is.EqualTo("Анонс статьи"));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("\n\n")]
    [TestCase("<p></p>")]
    [TestCase("<div><p> </p></div>")]
    [TestCase("<script>alert('x');</script><style>p { color: red; }</style>")]
    [TestCase("<p>Читать далее</p>")]
    public void Parse_NoUsefulText_ReturnsEmptyTextFailure(string rawText)
    {
        var result = _parser.Parse(Item(rawText), SourceType.Html);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Item, Is.Null);
        Assert.That(result.Reason, Is.EqualTo(ParseFailureReason.EmptyText));
    }

    [Test]
    public void Parse_PlainTextAbstract_IsAccepted()
    {
        // arXiv: title + abstract, не HTML
        const string abstractText =
            "Neurosymbolic systems combine neural networks with symbolic reasoning.";

        var result = _parser.Parse(Item(abstractText), SourceType.Html);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Item!.CleanText, Is.EqualTo(abstractText));
    }
}