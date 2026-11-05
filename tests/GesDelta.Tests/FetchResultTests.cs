using GesDelta.Core.Enums;
using GesDelta.Core.Models;
using NUnit.Framework;

namespace GesDelta.Tests;

public class FetchResultTests
{
    [Test]
    public void Success_SetsOkAndItems_WithoutReason()
    {
        var items = new List<FetchedItem>
        {
            new("id-1", "Title", "https://example.com/1", null, "raw", false)
        };

        var result = FetchResult.Success(items);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Reason, Is.Null);
    }

    [Test]
    public void Success_WithEmptyList_IsStillOk()
    {
        var result = FetchResult.Success([]);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Items, Is.Empty);
        Assert.That(result.Reason, Is.Null);
    }

    [TestCase(FetchFailureReason.Timeout)]
    [TestCase(FetchFailureReason.Http429)]
    [TestCase(FetchFailureReason.FormatChanged)]
    [TestCase(FetchFailureReason.RobotsDisallowed)]
    public void Failure_SetsReason_AndIsNotOk(FetchFailureReason reason)
    {
        var result = FetchResult.Failure(reason);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Reason, Is.EqualTo(reason));
    }

    [Test]
    public void Failure_ReturnsEmptyItems_NotNull()
    {
        var result = FetchResult.Failure(FetchFailureReason.Http404);

        Assert.That(result.Items, Is.Not.Null);
        Assert.That(result.Items, Is.Empty);
    }

    [Test]
    public void FetchResult_HasNoPublicConstructors()
    {
        var constructors = typeof(FetchResult).GetConstructors();

        Assert.That(constructors, Is.Empty,
            "FetchResult must be created only via Success/Failure");
    }
}