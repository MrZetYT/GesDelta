using GesDelta.Core.Enums;

namespace GesDelta.Core.Models;

public sealed record MaterialDetectionReport(
    long SourceId,
    string SourceName,
    string ExternalId,
    string Title,
    DetectionVerdict Verdict,
    long? ChangeId);