using System.Text.Json;
using GesDelta.Core.Enums;
using GesDelta.Core.Interfaces;
using GesDelta.Core.Models;
using GesDelta.Storage;

namespace GesDelta.Orchestrator;

public class MonitoringService(GesDeltaDbContext db, CollectRunner collect) : IMonitoringService
{
    public async Task<long> StartMonitoringAsync(StartMonitoringRequest request, CancellationToken ct)
    {
        Validate(request);

        var topic = new Topic
        {
            Name = request.TopicName,
            CheckInterval = request.CheckInterval,
            Enabled = true
        };

        var sources = request.Sources.Select(s => new Source
        {
            Topic = topic,
            Name = s.Name,
            Type = s.Type,
            Url = s.Url,
            Config = s.ConfigJson ?? "{}",
            Enabled = true
        });

        var document = new Document { Topic = topic, Kind = DocumentKind.Description };

        db.Add(topic);
        db.AddRange(sources);
        db.Add(document);

        await db.SaveChangesAsync(ct);

        return topic.Id;
    }
    
    public Task<CollectReport> CollectAsync(long topicId, CancellationToken ct) =>
        collect.CollectAsync(topicId, ct);

    private static void Validate(StartMonitoringRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TopicName))
            throw new ArgumentException("Topic name must not be empty");

        if (request.CheckInterval <= TimeSpan.Zero)
            throw new ArgumentException("Check interval must be greater than zero");

        if (request.Sources.Count is < 1 or > 5)
            throw new ArgumentException($"Sources count must be 1..5, got {request.Sources.Count}");

        foreach (var source in request.Sources)
        {
            if (string.IsNullOrWhiteSpace(source.Name))
                throw new ArgumentException("Source name must not be empty");

            if (!Uri.TryCreate(source.Url, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new ArgumentException($"Source '{source.Name}': URL must be absolute http(s)");

            if (source.ConfigJson is not null)
            {
                try { JsonDocument.Parse(source.ConfigJson).Dispose(); }
                catch (JsonException)
                {
                    throw new ArgumentException($"Source '{source.Name}': config is not valid JSON");
                }
            }
        }
    }
}