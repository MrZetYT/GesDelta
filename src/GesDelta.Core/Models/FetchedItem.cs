namespace GesDelta.Core.Models;

public record FetchedItem(
    string ExternalId,
    string Title,
    string Url,
    DateTime? PublishedAt,
    string RawText,
    bool FromPage);