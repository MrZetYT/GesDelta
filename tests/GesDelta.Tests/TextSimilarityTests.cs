using GesDelta.Tools;
using NUnit.Framework;

namespace GesDelta.Tests;

[TestFixture]
public class TextSimilarityTests
{
    private readonly TextSimilarity _similarity = new();

    [TestCase("hello", "hello", 1.0)]
    [TestCase("", "", 1.0)]
    [TestCase("hello", "", 0.0)]
    [TestCase("", "hello", 0.0)]
    [TestCase("abc", "xyz", 0.0)]
    [TestCase("hello", "hallo", 0.8)]
    [TestCase("abc", "abcd", 0.75)]
    [TestCase("abcd", "abc", 0.75)]
    public void Calculate_ReturnsExpectedSimilarity(
        string first,
        string second,
        double expected)
    {
        var result = _similarity.Calculate(first, second);

        Assert.That(result,
            Is.EqualTo(expected).Within(0.000001));
    }
}