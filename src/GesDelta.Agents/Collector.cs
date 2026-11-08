using System.Text.Json;
using GesDelta.Core.Enums;
using GesDelta.Core.Interfaces;
using GesDelta.Core.Models;
using GesDelta.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GesDelta.Agents;

public class Collector
{
    private record SourceConfig(int? MaxItems);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    
    private readonly Func<SourceType, ISourceFetcher?> _getFetcher;
    private readonly IOptions<SourcesOptions> _options;
    private readonly ILogger<Collector> _logger;

    public Collector(Func<SourceType, ISourceFetcher?> getFetcher,
        IOptions<SourcesOptions> options,
        ILogger<Collector> logger)
    {
        _getFetcher = getFetcher;
        _options = options;
        _logger = logger;
    }

    public async Task<FetchResult> CollectAsync(Source source, CancellationToken ct)
    {
        var configJson = string.IsNullOrWhiteSpace(source.Config) ? "{}" : source.Config;

        int maxItems;
        try
        {
            var config = JsonSerializer.Deserialize<SourceConfig>(configJson, JsonOptions);
            maxItems = config?.MaxItems ?? _options.Value.MaxItemsPerSource;
        }
        catch (JsonException)
        {
            _logger.LogWarning("Collector: source {SourceId} has invalid config JSON", source.Id);
            return FetchResult.Failure(FetchFailureReason.InvalidConfig);
        }

        if (maxItems <= 0)
        {
            _logger.LogWarning("Collector: source {SourceId} has non-positive maxItems {MaxItems}", source.Id, maxItems);
            return FetchResult.Failure(FetchFailureReason.InvalidConfig);
        }

        var fetcher = _getFetcher(source.Type);
        if (fetcher is null)
        {
            _logger.LogWarning("Collector: no fetcher for source {SourceId} of type {SourceType}", source.Id, source.Type);
            return FetchResult.Failure(FetchFailureReason.InvalidConfig);
        }

        var result = await fetcher.FetchAsync(
            new FetchRequest(source.Id, source.Type, source.Url, configJson, maxItems), ct);

        if (result.Ok)
            _logger.LogInformation("Collector: source {SourceId} returned {Count} items", source.Id, result.Items.Count);
        else
            _logger.LogWarning("Collector: source {SourceId} failed: {Reason}", source.Id, result.Reason);

        return result;
    }
}