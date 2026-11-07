using GesDelta.Tools;

namespace GesDelta.Tests;

public class UrlCanonicalizerTests
{
    [TestCase("https://habr.com/ru/articles/123/?utm_campaign=a&utm_source=b#habracut", "https://habr.com/ru/articles/123")]
    [TestCase("https://Habr.com/ru/articles/123/", "https://habr.com/ru/articles/123")]
    [TestCase("https://example.com/page?id=5&utm_medium=rss", "https://example.com/page?id=5")]
    [TestCase("https://example.com/page?UTM_SOURCE=x", "https://example.com/page")]
    [TestCase("https://example.com/page#section", "https://example.com/page")]
    [TestCase("https://example.com/", "https://example.com")]
    [TestCase("https://example.com", "https://example.com")]
    [TestCase(" https://example.com/a/ ", "https://example.com/a")]
    [TestCase("not a url", "not a url")]
    public void Canonicalize_KnownInputs_ReturnsExpected(string url, string expectedUrl)
    {
        var result = UrlCanonicalizer.Canonicalize(url);

        Assert.That(result, Is.EqualTo(expectedUrl));
    }
    
    [TestCase("https://habr.com/ru/articles/123/?utm_campaign=a&utm_source=b#habracut")]
    [TestCase("https://example.com/page?id=5&utm_medium=rss")]
    [TestCase("https://example.com/page?UTM_SOURCE=x")]
    [TestCase("https://example.com/page#section")]
    [TestCase("https://example.com/a/?id=5")]
    [TestCase("not a url")]
    public void Canonicalize_AppliedTwice_GivesSameResult(string url)
    {
        var firstCanonicalizing = UrlCanonicalizer.Canonicalize(url);
        var secondCanonicalizing = UrlCanonicalizer.Canonicalize(firstCanonicalizing);

        Assert.That(firstCanonicalizing, Is.EqualTo(secondCanonicalizing));
    }
}