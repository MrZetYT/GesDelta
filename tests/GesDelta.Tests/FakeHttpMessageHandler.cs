using System.Net;

namespace GesDelta.Tests;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly string _content;
    private readonly HttpStatusCode _statusCode;

    public FakeHttpMessageHandler(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _content = content;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_content)
        };
        return Task.FromResult(response);
    }
}