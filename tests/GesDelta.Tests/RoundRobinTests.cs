using GesDelta.Orchestrator;

namespace GesDelta.Tests;

[TestFixture]
public class RoundRobinTests
{
    [Test]
    public void Interleave_AlternatesBetweenGroups()
    {
        string[][] groups =
        [
            ["A1", "A2", "A3"],
            ["B1", "B2", "B3"],
            ["C1", "C2", "C3"]
        ];

        var result = RoundRobin.Interleave(groups)
            .ToArray();

        Assert.That(result, Is.EqualTo(new[]
        {
            "A1", "B1", "C1",
            "A2", "B2", "C2",
            "A3", "B3", "C3"
        }));
    }

    [Test]
    public void Interleave_HandlesUnequalGroups()
    {
        string[][] groups =
        [
            ["A1", "A2", "A3", "A4"],
            ["B1"],
            ["C1", "C2"]
        ];

        var result = RoundRobin.Interleave(groups)
            .ToArray();

        Assert.That(result, Is.EqualTo(new[]
        {
            "A1", "B1", "C1",
            "A2", "C2",
            "A3", "A4"
        }));
    }

    [Test]
    public void Interleave_SkipsEmptyGroups()
    {
        string[][] groups =
        [
            [],
            ["B1", "B2"],
            [],
            ["D1"]
        ];

        var result = RoundRobin.Interleave(groups)
            .ToArray();

        Assert.That(result, Is.EqualTo(new[]
        {
            "B1", "D1", "B2"
        }));
    }

    [Test]
    public void Interleave_HandlesNoGroups()
    {
        var result = RoundRobin.Interleave(
            Array.Empty<string[]>());

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Interleave_PreservesSingleGroupOrder()
    {
        string[][] groups =
        [
            ["A1", "A2", "A3"]
        ];

        var result = RoundRobin.Interleave(groups)
            .ToArray();

        Assert.That(result, Is.EqualTo(groups[0]));
    }

    [Test]
    public void Interleave_DistributesFirstTenFairly()
    {
        var groups = new[] { "A", "B", "C" }
            .Select(prefix => Enumerable.Range(1, 20)
                .Select(i => $"{prefix}{i}"));

        var result = RoundRobin.Interleave(groups)
            .Take(10)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Count(x => x.StartsWith("A")),
                Is.EqualTo(4));

            Assert.That(
                result.Count(x => x.StartsWith("B")),
                Is.EqualTo(3));

            Assert.That(
                result.Count(x => x.StartsWith("C")),
                Is.EqualTo(3));
        });
    }
}