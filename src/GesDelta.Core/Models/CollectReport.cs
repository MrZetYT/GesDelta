namespace GesDelta.Core.Models;

public record CollectReport(long RunId, IReadOnlyList<SourceCollectReport> Sources);