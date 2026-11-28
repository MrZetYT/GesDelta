using GesDelta.Core.Models;

namespace GesDelta.Orchestrator;

internal sealed record DetectionCandidate(
    long SourceId,
    ParsedItem Item);