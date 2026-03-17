using System.Net;
using System.Text;
using System.Text.Json;

namespace DriftDNS.Tests.Helpers;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();

    public int CallCount { get; private set; }

    public void Enqueue(HttpResponseMessage response) => _responses.Enqueue(response);

    public void EnqueueJson(object content) =>
        Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json")
        });

    public void EnqueueError(HttpStatusCode status = HttpStatusCode.InternalServerError) =>
        Enqueue(new HttpResponseMessage(status));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(_responses.TryDequeue(out var response)
            ? response
            : new HttpResponseMessage(HttpStatusCode.InternalServerError));
    }
}
