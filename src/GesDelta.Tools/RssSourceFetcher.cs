using System.ServiceModel.Syndication;
using System.Xml;
using GesDelta.Core.Enums;
using GesDelta.Core.Interfaces;
using GesDelta.Core.Models;

namespace GesDelta.Tools;

public class RssSourceFetcher : ISourceFetcher
{
    private readonly HttpClient _httpClient;

    public RssSourceFetcher(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<FetchResult> FetchAsync(FetchRequest fetchRequest, CancellationToken ct)
    {
        try
        {
            using var stream = await _httpClient.GetStreamAsync(fetchRequest.Url, ct);
            using var reader = XmlReader.Create(stream);
            var feed = SyndicationFeed.Load(reader);

            var items = new List<FetchedItem>();

            foreach (var entry in feed.Items)
            {
                var url = UrlCanonicalizer.Canonicalize(entry.Links.FirstOrDefault()?.Uri.AbsoluteUri ?? "");
                
                var externalId = string.IsNullOrWhiteSpace(entry.Id) ? url : entry.Id;

                if (string.IsNullOrWhiteSpace(externalId))
                    continue;

                items.Add(new FetchedItem(
                    ExternalId: externalId,
                    Title: entry.Title?.Text ?? "",
                    Url: url,
                    PublishedAt: entry.PublishDate == default ? null : entry.PublishDate.UtcDateTime,
                    RawText: entry.Summary?.Text ?? "",
                    FromPage: false));

                if (items.Count >= fetchRequest.MaxItems)
                    break;
            }

            return FetchResult.Success(items);
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