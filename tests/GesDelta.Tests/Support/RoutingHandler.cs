namespace GesDelta.Tests.Support;

public sealed class RoutingHandler(Func<Uri, HttpResponseMessage> route) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(route(request.RequestUri!));
}