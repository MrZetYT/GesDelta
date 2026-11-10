using GesDelta.Core.Enums;

namespace GesDelta.Core.Models;

public sealed record ParseResult
{
    public bool Ok { get; }
    public ParsedItem? Item { get; }
    public ParseFailureReason? Reason { get; }

    private ParseResult(bool ok, ParsedItem? item, ParseFailureReason? reason)
    {
        Ok = ok;
        Item = item;
        Reason = reason;
    }

    public static ParseResult Success(ParsedItem item) =>
        new(true, item, null);

    public static ParseResult Failure(ParseFailureReason reason) =>
        new(false, null, reason);
}