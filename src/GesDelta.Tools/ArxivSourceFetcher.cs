using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using GesDelta.Core.Enums;
using GesDelta.Core.Interfaces;
using GesDelta.Core.Models;

namespace GesDelta.Tools;

public class ArxivSourceFetcher : ISourceFetcher
{
    private readonly HttpClient _httpClient;
    private record ArxivConfig(string? Query);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);


    public ArxivSourceFetcher(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<FetchResult> FetchAsync(FetchRequest fetchRequest, CancellationToken ct)
    {
        try
        {
            var config = JsonSerializer.Deserialize<ArxivConfig>(fetchRequest.Config, JsonOptions);
            if (string.IsNullOrWhiteSpace(config?.Query))
                return FetchResult.Failure(FetchFailureReason.InvalidConfig);
            var query = Uri.EscapeDataString(config.Query);
            var streamUrl =
                $"{fetchRequest.Url}?search_query=all:{query}&start=0&max_results={fetchRequest.MaxItems}&sortBy=submittedDate&sortOrder=descending";
            using var stream = await _httpClient.GetStreamAsync(streamUrl, ct);
            XNamespace atom = "http://www.w3.org/2005/Atom";
            var doc = XDocument.Load(stream);

            var items = new List<FetchedItem>();

            foreach (var entry in doc.Descendants(atom + "entry"))
            {
                var id = entry.Element(atom + "id")?.Value ?? "";
                const string absMarker = "/abs/";
                var markerIndex = id.IndexOf(absMarker, StringComparison.Ordinal);
                if (markerIndex < 0)
                    continue;
                var withVersion = id[(markerIndex + absMarker.Length)..];
                var externalId = Regex.Replace(withVersion, @"v\d+$", "");

                if (string.IsNullOrWhiteSpace(externalId))
                    continue;

                var title = entry.Element(atom + "title")?.Value ?? "";

                var summary = entry.Element(atom + "summary")?.Value ?? "";

                var url = entry.Elements(atom + "link")
                    .FirstOrDefault(l => (string?)l.Attribute("rel") == "alternate")
                    ?.Attribute("href")?.Value ?? "";

                var publishedText = entry.Element(atom + "published")?.Value;
                DateTime? publishedAt = DateTimeOffset.TryParse(publishedText, out var parsed)
                    ? parsed.UtcDateTime
                    : null;

                items.Add(new FetchedItem(
                    ExternalId: externalId,
                    Title: title,
                    Url: url,
                    PublishedAt: publishedAt,
                    RawText: $"{title}\n\n{summary}",
                    FromPage: false));

                if (items.Count >= fetchRequest.MaxItems)
                    break;
            }

            return FetchResult.Success(items);
        }
        catch (JsonException)
        {
            return FetchResult.Failure(FetchFailureReason.InvalidConfig);
        }
        catch (XmlException)
        {
            return FetchResult.Failure(FetchFailureReason.FormatChanged);
        }
        catch (HttpRequestException ex)
        {
            return FetchResult.Failure(MapHttpError(ex));
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return FetchResult.Failure(FetchFailureReason.Timeout);
        }
    }

    private static FetchFailureReason MapHttpError(HttpRequestException ex)
    {
        if (ex.StatusCode is null)
            return FetchFailureReason.Timeout;

        return (int)ex.StatusCode.Value switch
        {
            404 or 410 => FetchFailureReason.Http404,
            401 or 403 => FetchFailureReason.Http403,
            429 => FetchFailureReason.Http429,
            _ => FetchFailureReason.Http5xx
        };
    }
}