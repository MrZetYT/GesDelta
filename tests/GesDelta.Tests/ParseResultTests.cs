using GesDelta.Core.Enums;
using GesDelta.Core.Models;

namespace GesDelta.Tests;

public class ParseResultTests
{
    private static ParsedItem CreateItem() => new(
        ExternalId: "hep-th/9901001",
        Title: "Title",
        Url: "https://example.com/a",
        PublishedAt: null,
        CleanText: "Text",
        ContentHash: "abc123");

    [Test]
    public void Success_HasItemAndNoReason()
    {
        var item = CreateItem();

        var result = ParseResult.Success(item);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Item, Is.SameAs(item));
        Assert.That(result.Reason, Is.Null);
    }

    [TestCase(ParseFailureReason.EmptyText)]
    [TestCase(ParseFailureReason.Unsupported)]
    public void Failure_HasReasonAndNoItem(ParseFailureReason reason)
    {
        var result = ParseResult.Failure(reason);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Item, Is.Null);
        Assert.That(result.Reason, Is.EqualTo(reason));
    }

    [Test]
    public void ParsedItem_SameValues_AreEqual()
    {
        Assert.That(CreateItem(), Is.EqualTo(CreateItem()));
    }
}