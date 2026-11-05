using GesDelta.Core.Enums;

namespace GesDelta.Core.Models;

public sealed record FetchResult
{
    public bool Ok { get; }
    public IReadOnlyList<FetchedItem> Items { get; }
    public FetchFailureReason? Reason { get; }

    private FetchResult(bool ok, IReadOnlyList<FetchedItem> items, FetchFailureReason? reason)
    {
        Ok = ok;
        Items = items;
        Reason = reason;
    }

    public static FetchResult Success(IReadOnlyList<FetchedItem> items) =>
        new(true, items, null);

    public static FetchResult Failure(FetchFailureReason reason) =>
        new(false, [], reason);
}