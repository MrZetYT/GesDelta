using GesDelta.Core.Enums;

namespace GesDelta.Core.Models;

public sealed record DetectionResult(
    DetectionVerdict Verdict,
    long? ChangeId = null,
    IReadOnlyList<string>? AddedFragments = null);