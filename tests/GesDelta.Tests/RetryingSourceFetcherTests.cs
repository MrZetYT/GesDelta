using GesDelta.Core.Enums;
using GesDelta.Core.Interfaces;
using GesDelta.Core.Models;
using GesDelta.Core.Options;
using GesDelta.Tools;
using Microsoft.Extensions.Options;

namespace GesDelta.Tests;

public class RetryingSourceFetcherTests
{
    private sealed class ScriptedFetcher : ISourceFetcher
    {
        private readonly FetchResult[] _results;
        public int Calls { get; private set; }

        public ScriptedFetcher(params FetchResult[] results)
        {
            _results = results;
        }

        public Task<FetchResult> FetchAsync(FetchRequest fetchRequest, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var result = _results[Math.Min(Calls, _results.Length - 1)];
            Calls++;
            return Task.FromResult(result);
        }
    }

    private static FetchResult Ok() => FetchResult.Success(new List<FetchedItem>());
    private static FetchResult Fail(FetchFailureReason reason) => FetchResult.Failure(reason);

    private static FetchRequest Request(string url = "https://habr.com/ru/rss/feed") =>
        new(1, SourceType.Rss, url, "", 10);

    private static (RetryingSourceFetcher Sut, HostThrottle Throttle) Create(
        ISourceFetcher inner, int[]? pausesSec = null, int habrDelayMs = 0)
    {
        var sources = new SourcesOptions
        {
            HostDelayMs = 0,
            HostDelaysMs = { ["habr.com"] = habrDelayMs }
        };
        var throttle = new HostThrottle(Options.Create(sources), TimeProvider.System);
        var collector = new CollectorOptions { PausesSec = pausesSec ?? [0, 0, 0] };

        var sut = new RetryingSourceFetcher(inner, throttle, Options.Create(collector), TimeProvider.System);
        return (sut, throttle);
    }

    [Test]
    public async Task Fetch_ImmediateSuccess_CallsOnce()
    {
        var inner = new ScriptedFetcher(Ok());
        var (sut, _) = Create(inner);

        var result = await sut.FetchAsync(Request(), CancellationToken.None);

        Assert.That(result.Ok, Is.True);
        Assert.That(inner.Calls, Is.EqualTo(1));
    }

    [Test]
    public async Task Fetch_TwoServerErrorsThenSuccess_CallsThreeTimes()
    {
        var inner = new ScriptedFetcher(
            Fail(FetchFailureReason.Http5xx), Fail(FetchFailureReason.Http5xx), Ok());
        var (sut, _) = Create(inner);

        var result = await sut.FetchAsync(Request(), CancellationToken.None);

        Assert.That(result.Ok, Is.True);
        Assert.That(inner.Calls, Is.EqualTo(3));
    }

    [Test]
    public async Task Fetch_RateLimitThenSuccess_CallsTwice()
    {
        var inner = new ScriptedFetcher(Fail(FetchFailureReason.Http429), Ok());
        var (sut, _) = Create(inner);

        var result = await sut.FetchAsync(Request(), CancellationToken.None);

        Assert.That(result.Ok, Is.True);
        Assert.That(inner.Calls, Is.EqualTo(2));
    }

    [TestCase(FetchFailureReason.Timeout)]
    [TestCase(FetchFailureReason.Http5xx)]
    [TestCase(FetchFailureReason.Http429)]
    public async Task Fetch_AlwaysTransient_GivesUpAfterFourCalls(FetchFailureReason reason)
    {
        var inner = new ScriptedFetcher(Fail(reason));
        var (sut, _) = Create(inner);

        var result = await sut.FetchAsync(Request(), CancellationToken.None);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Reason, Is.EqualTo(reason));
        Assert.That(inner.Calls, Is.EqualTo(4));
    }

    [TestCase(FetchFailureReason.Http404)]
    [TestCase(FetchFailureReason.Http403)]
    [TestCase(FetchFailureReason.FormatChanged)]
    [TestCase(FetchFailureReason.InvalidConfig)]
    public async Task Fetch_PermanentFailure_DoesNotRetry(FetchFailureReason reason)
    {
        var inner = new ScriptedFetcher(Fail(reason));
        var (sut, _) = Create(inner);

        var result = await sut.FetchAsync(Request(), CancellationToken.None);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Reason, Is.EqualTo(reason));
        Assert.That(inner.Calls, Is.EqualTo(1));
    }

    [Test]
    public async Task Fetch_NoPausesConfigured_DoesNotRetry()
    {
        var inner = new ScriptedFetcher(Fail(FetchFailureReason.Timeout));
        var (sut, _) = Create(inner, pausesSec: []);

        var result = await sut.FetchAsync(Request(), CancellationToken.None);

        Assert.That(result.Ok, Is.False);
        Assert.That(inner.Calls, Is.EqualTo(1));
    }

    [Test]
    public async Task Fetch_UnparsableUrl_ReturnsInvalidConfigWithoutCallingInner()
    {
        var inner = new ScriptedFetcher(Ok());
        var (sut, _) = Create(inner);

        var result = await sut.FetchAsync(Request("not a url"), CancellationToken.None);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Reason, Is.EqualTo(FetchFailureReason.InvalidConfig));
        Assert.That(inner.Calls, Is.EqualTo(0));
    }

    [Test]
    public void Fetch_CancelledToken_ThrowsInsteadOfReturningFailure()
    {
        var inner = new ScriptedFetcher(Ok());
        var (sut, _) = Create(inner);
        var cancelled = new CancellationToken(canceled: true);

        Assert.That(
            async () => await sut.FetchAsync(Request(), cancelled),
            Throws.InstanceOf<OperationCanceledException>());
        Assert.That(inner.Calls, Is.EqualTo(0));
    }

    [Test]
    public async Task Fetch_MarksHostNotFullUrl_ForThrottle()
    {
        var inner = new ScriptedFetcher(Ok());
        var (sut, throttle) = Create(inner, habrDelayMs: 10000);

        await sut.FetchAsync(Request("https://habr.com/ru/rss/hubs/machine_learning/"), CancellationToken.None);

        Assert.That(throttle.GetWait("habr.com", DateTimeOffset.UtcNow), Is.GreaterThan(TimeSpan.Zero));
    }
}