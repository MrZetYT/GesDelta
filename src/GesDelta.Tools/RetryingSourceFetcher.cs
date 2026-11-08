using GesDelta.Core.Enums;
using GesDelta.Core.Interfaces;
using GesDelta.Core.Models;
using GesDelta.Core.Options;
using Microsoft.Extensions.Options;

namespace GesDelta.Tools;

public class RetryingSourceFetcher : ISourceFetcher
{
    private readonly ISourceFetcher _inner;
    private readonly HostThrottle _hostThrottle;
    private readonly IOptions<CollectorOptions> _collectorOptions;
    private readonly TimeProvider _timeProvider;

    public RetryingSourceFetcher(ISourceFetcher inner,
        HostThrottle hostThrottle,
        IOptions<CollectorOptions> collectorOptions,
        TimeProvider timeProvider)
    {
        _inner = inner;
        _hostThrottle = hostThrottle;
        _collectorOptions = collectorOptions;
        _timeProvider = timeProvider;
    }
    
    private static bool IsTransient(FetchFailureReason? reason) =>
        reason is FetchFailureReason.Timeout or FetchFailureReason.Http5xx or FetchFailureReason.Http429;

    public async Task<FetchResult> FetchAsync(FetchRequest fetchRequest, CancellationToken ct)
    {
        if (!Uri.TryCreate(fetchRequest.Url, UriKind.Absolute, out var uri))
            return FetchResult.Failure(FetchFailureReason.InvalidConfig);
        
        var pauses = _collectorOptions.Value.PausesSec;

        for (var attempt = 0; ; attempt++)
        {
            await _hostThrottle.WaitAsync(uri.Host, ct);
            var result = await _inner.FetchAsync(fetchRequest, ct);

            if (result.Ok || !IsTransient(result.Reason) || attempt >= pauses.Length)
                return result;

            await Task.Delay(TimeSpan.FromSeconds(pauses[attempt]), _timeProvider, ct);
        }
    }
}