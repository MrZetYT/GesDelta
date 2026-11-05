using GesDelta.Core.Enums;

namespace GesDelta.Core.Models;

public record FetchRequest(
    long SourceId,
    SourceType SourceType,
    string Url,
    string Config,
    int MaxItems);