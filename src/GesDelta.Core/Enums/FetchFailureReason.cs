namespace GesDelta.Core.Enums;

public enum FetchFailureReason
{
    Timeout,
    Http5xx,
    Http429,
    FormatChanged,
    Http404,
    Http403,
    RobotsDisallowed
}