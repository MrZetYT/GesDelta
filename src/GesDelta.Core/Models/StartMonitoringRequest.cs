namespace GesDelta.Core.Models;

public record StartMonitoringRequest(
    string TopicName,
    TimeSpan CheckInterval,
    IReadOnlyList<SourceSetup> Sources);