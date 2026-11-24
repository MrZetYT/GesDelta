namespace GesDelta.Core.Models;

public record SourceCollectReport(
    string SourceName,
    bool FetchOk,
    string? FailureReason,
    int Fetched,
    int ParseFailed,
    int NewMaterials,
    int NewSnapshots,
    int Unchanged);