namespace GesDelta.Core.Models;

public record ParsedItem(
    string ExternalId,
    string Title,
    string Url,
    DateTime? PublishedAt,
    string CleanText,
    string ContentHash);