namespace GesDelta.Core.Models;

public sealed record SignificanceResult(
    bool IsSignificant,
    IReadOnlyList<string> AddedFragments);