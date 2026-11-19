using System.IO;
using System.Xml.Linq;
using GesDelta.Agents;

namespace GesDelta.Tests;

public class HtmlToTextTests
{
    
    [Test]
    public void Convert_DecodesHtmlEntities()
    {
        const string html = "<p>Привет &amp; пока</p>";

        var result = HtmlToText.Convert(html);

        Assert.That(result, Is.EqualTo("Привет & пока"));
    }
    
    
    [Test]
    public void Convert_RemovesScriptsAndStyles()
    {
        const string html =
            "<p>Привет</p>" +
            "<script>alert('test');</script>" +
            "<style>.test { color: red; }</style>";

        var result = HtmlToText.Convert(html);

        Assert.That(result, Is.EqualTo("Привет"));
        Assert.That(result, Does.Not.Contain("alert"));
        Assert.That(result, Does.Not.Contain("color"));
    }

    
    [Test]
    public void Convert_PreservesParagraphBoundaries()
    {
        const string html = "<p>Первый абзац</p><p>Второй абзац</p>";

        var result = HtmlToText.Convert(html);

        Assert.That(result, Does.Contain("Первый абзац"));
        Assert.That(result, Does.Contain("Второй абзац"));
        Assert.That(result, Does.Contain("\n\n"));
    }

    [Test]
    public void Convert_PreservesLineBreaks()
    {
        const string html = "Первая строка<br>Вторая строка";

        var result = HtmlToText.Convert(html);

        Assert.That(result, Does.Contain("Первая строка"));
        Assert.That(result, Does.Contain("Вторая строка"));
        Assert.That(result, Does.Contain("\n\n"));
    }

    
    [Test]
    public void Convert_HandlesNestedBlocks()
    {
        const string html =
            "<div><p>Первый абзац</p><p>Второй абзац</p></div>";

        var result = HtmlToText.Convert(html);

        Assert.That(result, Does.Contain("Первый абзац"));
        Assert.That(result, Does.Contain("Второй абзац"));
        Assert.That(result, Does.Contain("\n\n"));
    }

    [Test]
    public void Convert_HandlesInlineElements()
    {
        const string html = "<p>Привет <b>мир</b>!</p>";

        var result = HtmlToText.Convert(html);

        Assert.That(result, Is.EqualTo("Привет мир!"));
    }

    [Test]
    public void Convert_HandlesMultipleParagraphs()
    {
        const string html =
            "<p>А</p><p>Б</p><p>В</p>";

        var result = HtmlToText.Convert(html);

        Assert.That(result, Does.Contain("А"));
        Assert.That(result, Does.Contain("Б"));
        Assert.That(result, Does.Contain("В"));
        Assert.That(result, Does.Contain("\n\n"));
    }

    [TestCase("habr_feed.xml")]
    public void Convert_HabrFeed_DoesNotContainReadMore(string fileName)
    {
        var filePath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Data",
            fileName
        );

        Assert.That(File.Exists(filePath), Is.True,
            $"Файл {fileName} не найден");

        var xml = File.ReadAllText(filePath);
        var document = XDocument.Parse(xml);

        var texts = document.Descendants()
            .Where(x => x.Name.LocalName is "description" or "content")
            .Select(x => TextNormalizer.Normalize(HtmlToText.Convert(x.Value)))
            .Where(t => t.Length > 0)
            .ToList();

        Assert.That(texts, Is.Not.Empty);
        Assert.That(texts, Has.None.Contain("Читать далее"));
    }
}