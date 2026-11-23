using GesDelta.Core.Models;

namespace GesDelta.Core.Interfaces;

public interface IMonitoringService
{
    Task<long> StartMonitoringAsync(StartMonitoringRequest request, CancellationToken ct);
}