using System.Net;
using System.Net.Http.Headers;

namespace Packer.Tests;

internal sealed class TestHttpMessageHandler(IReadOnlyDictionary<string, TestHttpResource> resources)
    : HttpMessageHandler
{
    public List<string> Requests { get; } = [];

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string location = request.RequestUri?.ToString() ?? string.Empty;

        if (request.Headers.Authorization is null)
        {
            Requests.Add($"{request.Method} {location}");
        }
        else
        {
            Requests.Add($"{request.Method} {location} {request.Headers.Authorization}");
        }

        if (!resources.TryGetValue(location, out TestHttpResource? resource))
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new ByteArrayContent([]) };
        }

        if (resource.RedirectsTo is not null)
        {
            return BuildRedirect(resource);
        }
        else if (request.Method == HttpMethod.Head)
        {
            return BuildHead(resource);
        }
        else
        {
            return BuildContent(request, resource);
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Send(request, cancellationToken);
    }

    private static void AddHeaders(HttpResponseMessage response, TestHttpResource resource)
    {
        foreach (KeyValuePair<string, string> header in resource.Headers)
        {
            response.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    private static HttpResponseMessage BuildRedirect(TestHttpResource resource)
    {
        HttpResponseMessage response = new(HttpStatusCode.Redirect) { Content = new ByteArrayContent([]) };

        response.Headers.Location = new Uri(resource.RedirectsTo!);
        AddHeaders(response, resource);

        return response;
    }

    private static HttpResponseMessage BuildHead(TestHttpResource resource)
    {
        HttpResponseMessage response = new(HttpStatusCode.OK) { Content = new ByteArrayContent([]) };

        response.Content.Headers.ContentLength = resource.Contents.Length;
        AddHeaders(response, resource);

        return response;
    }

    private static HttpResponseMessage BuildContent(HttpRequestMessage request, TestHttpResource resource)
    {
        RangeHeaderValue? range = request.Headers.Range;
        int offset = 0;

        if (range is not null)
        {
            RangeItemHeaderValue item = range.Ranges.First();

            offset = (int)(item.From ?? 0);
        }

        HttpStatusCode status = HttpStatusCode.OK;

        if (offset > 0)
        {
            status = HttpStatusCode.PartialContent;
        }

        HttpResponseMessage response = new(status) { Content = new ByteArrayContent(resource.Contents[offset..]) };

        AddHeaders(response, resource);

        return response;
    }
}
