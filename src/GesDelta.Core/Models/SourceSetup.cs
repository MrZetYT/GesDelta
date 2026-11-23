using GesDelta.Core.Enums;

namespace GesDelta.Core.Models;

public record SourceSetup(string Name, SourceType Type, string Url, string? ConfigJson);
