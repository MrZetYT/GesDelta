using GesDelta.Agents;
using GesDelta.Core.Options;
using GesDelta.Tools;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace GesDelta.Tests;

[TestFixture]
public class SignificanceJudgeTests
{
    private SignificanceJudge _judge = null!;

    [SetUp]
    public void SetUp()
    {
        _judge = new SignificanceJudge(
            new DiffPlexTextDiffer(),
            new TextSimilarity(),
            Options.Create(new DetectorOptions()));
    }

    [Test]
    public void IdenticalTexts_AreNotSignificant()
    {
        var result = _judge.Evaluate(
            "Original paragraph",
            "Original paragraph");

        Assert.That(result.IsSignificant, Is.False);
        Assert.That(result.AddedFragments, Is.Empty);
    }

    [Test]
    public void ShortParagraph_IsIgnored()
    {
        var result = _judge.Evaluate(
            "Original paragraph",
            "Original paragraph\nShort addition");

        Assert.That(result.IsSignificant, Is.False);
        Assert.That(result.AddedFragments, Is.Empty);
    }

    [TestCase(39, false, 0)]
    [TestCase(40, false, 1)]
    [TestCase(199, false, 1)]
    [TestCase(200, true, 1)]
    [TestCase(201, true, 1)]
    public void AddedParagraph_RespectsThresholds(
        int length,
        bool expectedSignificance,
        int expectedCount)
    {
        var paragraph = new string('x', length);

        var result = _judge.Evaluate(
            "Original paragraph",
            $"Original paragraph\n{paragraph}");

        Assert.That(result.IsSignificant,
            Is.EqualTo(expectedSignificance));

        Assert.That(result.AddedFragments,
            Has.Count.EqualTo(expectedCount));
    }

    [Test]
    public void MinorEdit_IsNotNewInformation()
    {
        var baseline = new string('a', 250);
        var current = new string('a', 249) + "b";

        var result = _judge.Evaluate(baseline, current);

        Assert.That(result.IsSignificant, Is.False);
        Assert.That(result.AddedFragments, Is.Empty);
    }

    [Test]
    public void DeletedParagraph_IsNotSignificant()
    {
        var paragraph = new string('a', 250);

        var result = _judge.Evaluate(
            $"Original paragraph\n{paragraph}",
            "Original paragraph");

        Assert.That(result.IsSignificant, Is.False);
        Assert.That(result.AddedFragments, Is.Empty);
    }

    [Test]
    public void AccumulatedAdditions_BecomeSignificant()
    {
        const string baseline = "Original paragraph";

        var first = new string('x', 150);
        var second = new string('y', 150);

        var firstResult = _judge.Evaluate(
            baseline,
            $"{baseline}\n{first}");

        var secondResult = _judge.Evaluate(
            baseline,
            $"{baseline}\n{first}\n{second}");

        Assert.That(firstResult.IsSignificant, Is.False);
        Assert.That(secondResult.IsSignificant, Is.True);

        Assert.That(secondResult.AddedFragments,
            Is.EqualTo(new[] { first, second }));
    }
}