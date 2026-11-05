using GesDelta.Core.Models;

namespace GesDelta.Core.Interfaces;

public interface ISourceFetcher
{
    Task<FetchResult> FetchAsync(FetchRequest fetchRequest, CancellationToken ct);
}