using GesDelta.Core.Enums;

namespace GesDelta.Core.Models;

public sealed record ParseResult
{
    public bool Ok => Item is not null;
    public ParsedItem? Item { get; }
    public ParseFailureReason? Reason { get; }

    private ParseResult(ParsedItem? item, ParseFailureReason? reason)
    {
        Item = item;
        Reason = reason;
    }

    public static ParseResult Success(ParsedItem item) =>
        new(item, null);

    public static ParseResult Failure(ParseFailureReason reason) =>
        new(null, reason);
}