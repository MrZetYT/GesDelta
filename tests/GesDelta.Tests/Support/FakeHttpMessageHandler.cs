using System.Net;

namespace GesDelta.Tests;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly string _content;
    private readonly HttpStatusCode _statusCode;

    public Uri? LastRequestUri { get; private set; }

    public FakeHttpMessageHandler(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _content = content;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;

        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_content)
        };
        return Task.FromResult(response);
    }
}