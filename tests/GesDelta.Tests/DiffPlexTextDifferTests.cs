using GesDelta.Tools;
using NUnit.Framework;

namespace GesDelta.Tests;

[TestFixture]
public class DiffPlexTextDifferTests
{
    private readonly DiffPlexTextDiffer _differ = new();

    [Test]
    public void IdenticalTexts_ReturnsEmpty()
    {
        var result = _differ.GetAddedParagraphs(
            "First\nSecond",
            "First\nSecond");

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void AddedParagraph_ReturnsParagraph()
    {
        var result = _differ.GetAddedParagraphs(
            "First\nSecond",
            "First\nNew paragraph\nSecond");

        Assert.That(result,
            Is.EqualTo(new[] { "New paragraph" }));
    }

    [Test]
    public void DeletedParagraph_ReturnsEmpty()
    {
        var result = _differ.GetAddedParagraphs(
            "First\nSecond\nThird",
            "First\nThird");

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ModifiedParagraph_ReturnsNewVersion()
    {
        var result = _differ.GetAddedParagraphs(
            "Accuracy is 85%.",
            "Accuracy is 87%.");

        Assert.That(result,
            Is.EqualTo(new[] { "Accuracy is 87%." }));
    }

    [Test]
    public void MultipleAdditions_ReturnsInOrder()
    {
        var result = _differ.GetAddedParagraphs(
            "First\nSecond",
            "First\nNew A\nSecond\nNew B");

        Assert.That(result,
            Is.EqualTo(new[] { "New A", "New B" }));
    }
}