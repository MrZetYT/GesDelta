using System.Net;
using System.Text;

namespace GesDelta.Tests.Support;

public sealed class FileResponseHandler(string filePath) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                File.ReadAllText(filePath), Encoding.UTF8, "application/xml")
        });
}