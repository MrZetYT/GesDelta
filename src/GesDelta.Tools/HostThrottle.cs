using GesDelta.Core.Options;
using Microsoft.Extensions.Options;

namespace GesDelta.Tools;

public class HostThrottle
{
    private readonly Dictionary<string, DateTimeOffset> _lastRequestAt = new();
    private readonly SourcesOptions _options;
    private readonly TimeProvider _timeProvider;

    public HostThrottle(IOptions<SourcesOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }
    
    public TimeSpan GetWait(string host, DateTimeOffset now)
    {
        if (!_lastRequestAt.TryGetValue(host, out var last))
            return TimeSpan.Zero;

        var readyAt = last + TimeSpan.FromMilliseconds(_options.GetDelayMs(host));
        var wait = readyAt - now;
        return wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
    }
    
    public void Mark(string host, DateTimeOffset moment)
    {
        _lastRequestAt[host] = moment;
    }

    public async Task WaitAsync(string host, CancellationToken ct)
    {
        var wait = GetWait(host, _timeProvider.GetUtcNow());
        if (wait > TimeSpan.Zero)
            await Task.Delay(wait, _timeProvider, ct);

        Mark(host, _timeProvider.GetUtcNow());
    }
}