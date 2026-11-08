using GesDelta.Core.Options;
using GesDelta.Tools;
using Microsoft.Extensions.Options;

namespace GesDelta.Tests;

public class HostThrottleTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private static HostThrottle CreateThrottle()
    {
        var options = new SourcesOptions
        {
            HostDelayMs = 1000,
            HostDelaysMs =
            {
                ["habr.com"] = 10000,
                ["export.arxiv.org"] = 3000
            }
        };
        return new HostThrottle(Options.Create(options), TimeProvider.System);
    }

    [Test]
    public void GetWait_HostNeverSeen_ReturnsZero()
    {
        var throttle = CreateThrottle();

        Assert.That(throttle.GetWait("habr.com", T0), Is.EqualTo(TimeSpan.Zero));
    }

    [TestCase(0, 10)]
    [TestCase(4, 6)]
    [TestCase(10, 0)]
    [TestCase(30, 0)]
    public void GetWait_AfterMark_ReturnsRemainingDelay(int secondsLater, int expectedWaitSeconds)
    {
        var throttle = CreateThrottle();
        throttle.Mark("habr.com", T0);

        var wait = throttle.GetWait("habr.com", T0.AddSeconds(secondsLater));

        Assert.That(wait, Is.EqualTo(TimeSpan.FromSeconds(expectedWaitSeconds)));
    }

    [Test]
    public void GetWait_DifferentHost_IsIndependent()
    {
        var throttle = CreateThrottle();
        throttle.Mark("habr.com", T0);

        Assert.That(throttle.GetWait("export.arxiv.org", T0), Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void GetWait_HostNotInDictionary_UsesDefaultDelay()
    {
        var throttle = CreateThrottle();
        throttle.Mark("example.com", T0);

        Assert.That(throttle.GetWait("example.com", T0), Is.EqualTo(TimeSpan.FromMilliseconds(1000)));
    }

    [Test]
    public void GetWait_MarkedTwice_UsesLatestMark()
    {
        var throttle = CreateThrottle();
        throttle.Mark("habr.com", T0);
        throttle.Mark("habr.com", T0.AddSeconds(8));

        // Отсчёт идёт от второй отметки: в T0 + 8 с ждать надо полные 10 секунд
        Assert.That(throttle.GetWait("habr.com", T0.AddSeconds(8)), Is.EqualTo(TimeSpan.FromSeconds(10)));
    }
}