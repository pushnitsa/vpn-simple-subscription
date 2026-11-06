using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using VPN.SimpleSub.Web.Constants;
using VPN.SimpleSub.Web.Models;
using VPN.SimpleSub.Web.Services;

namespace VPN.SimpleSub.Test;

public class FetchSubscriptionIntegrationTests
{
    private const string UpstreamUrl = "https://upstream.example.com/sub/token123";
    private const string Node1Plain =
        "vless://00000000-0000-0000-0000-000000000000@198.51.100.1:443?type=tcp&security=reality&sni=sni.example.com#OrigTitle1";
    private const string Node2Plain =
        "vless://00000000-0000-0000-0000-000000000001@198.51.100.2:8443?type=ws#OrigTitle2";

    private static (SubscriptionService Service, TestHttpMessageHandler HttpHandler) CreateService(
        List<VpnConnection> connections,
        HttpResponseMessage response)
    {
        var httpHandler = new TestHttpMessageHandler(response);
        var httpClient = new HttpClient(httpHandler);
        var httpClientFactory = new TestHttpClientFactory(httpClient);

        var uriEditor = new VlessConfigEditor();
        var fetcher = new SubscriptionFetcher(httpClientFactory);
        var fetchEngine = new FetchSubscriptionEngine(fetcher, uriEditor);

        var options = Options.Create(new AppSettings
        {
            SubscriptionStorageDiscoveryPath = "dummy-path",
            SubscriptionRoute = "sub"
        });

        var document = new SubscriptionsDocument { Subscriptions = connections };
        var reader = new FakeDocumentReader(document);

        var service = new SubscriptionService(options, [fetchEngine], reader);
        return (service, httpHandler);
    }

    private static HttpResponseMessage CreateHttpResponse(
        HttpStatusCode statusCode,
        string content,
        Dictionary<string, string>? headers = null)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "text/plain")
        };

        if (headers != null)
        {
            foreach (var (key, value) in headers)
            {
                response.Headers.TryAddWithoutValidation(key, value);
            }
        }

        return response;
    }

    [Fact]
    public async Task GetSubscriptionAsync_ShouldFetchUpstreamDecodeBase64ApplyOverridesAndReturnResponse()
    {
        // Arrange: Upstream responds with base64-encoded configs and subscription headers
        var upstreamContent = $"{Node1Plain}\n{Node2Plain}";
        var upstreamBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(upstreamContent));

        var upstreamHeaders = new Dictionary<string, string>
        {
            [SubscriptionHeaders.UserInfo] = "upload=1000; download=2000; total=50000; expire=1735689600",
            [SubscriptionHeaders.UpdateInterval] = "6",
            [SubscriptionHeaders.Title] = "base64:VGVzdFRpdGxl"
        };
        var httpResponse = CreateHttpResponse(HttpStatusCode.OK, upstreamBase64, upstreamHeaders);

        var connections = new List<VpnConnection>
        {
            new()
            {
                ClientId = "client-fetch",
                Engine = "fetch",
                SubscriptionUrl = UpstreamUrl,
                Overrides =
                [
                    new VpnConnectionOverride
                    {
                        MatchSni = "sni.example.com",
                        Title = "Reality Fast",
                        Host = "proxy.example.com",
                        Port = 20443
                    }
                ]
            }
        };

        var (service, httpHandler) = CreateService(connections, httpResponse);

        // Act
        var result = await service.GetSubscriptionAsync("client-fetch");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(httpHandler.LastRequest);
        Assert.Equal(UpstreamUrl, httpHandler.LastRequest.RequestUri?.ToString());

        // Verify decoded body
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(result.Content));
        var lines = decoded.Split('\n');
        Assert.Equal(2, lines.Length);

        // Node 1: Overridden host, port, title, preserved sni
        Assert.Contains("proxy.example.com:20443", lines[0]);
        Assert.Contains("#Reality Fast", lines[0]);
        Assert.Contains("sni=sni.example.com", lines[0]);

        // Node 2: Untouched pass-through
        Assert.Equal(Node2Plain, lines[1]);

        // Verify forwarded headers
        Assert.Equal("upload=1000; download=2000; total=50000; expire=1735689600", result.Headers[SubscriptionHeaders.UserInfo]);
        Assert.Equal("6", result.Headers[SubscriptionHeaders.UpdateInterval]);
        Assert.Equal("base64:VGVzdFRpdGxl", result.Headers[SubscriptionHeaders.Title]);
    }

    [Fact]
    public async Task GetSubscriptionAsync_ShouldApplyFallbackUpdateInterval_WhenUpstreamOmitsInterval()
    {
        // Arrange
        var upstreamBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(Node1Plain));
        var upstreamHeaders = new Dictionary<string, string>
        {
            [SubscriptionHeaders.UserInfo] = "upload=500; download=500; total=2000; expire=0"
        };
        var httpResponse = CreateHttpResponse(HttpStatusCode.OK, upstreamBase64, upstreamHeaders);

        var connections = new List<VpnConnection>
        {
            new()
            {
                ClientId = "client-no-interval",
                Engine = "fetch",
                SubscriptionUrl = UpstreamUrl
            }
        };

        var (service, _) = CreateService(connections, httpResponse);

        // Act
        var result = await service.GetSubscriptionAsync("client-no-interval");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SubscriptionHeaders.DefaultUpdateInterval, result.Headers[SubscriptionHeaders.UpdateInterval]);
        Assert.Equal("upload=500; download=500; total=2000; expire=0", result.Headers[SubscriptionHeaders.UserInfo]);
    }

    [Fact]
    public async Task GetSubscriptionAsync_ShouldHandlePlainVlessBodyFromUpstream()
    {
        // Arrange: Upstream responds with plain text (not base64)
        var httpResponse = CreateHttpResponse(HttpStatusCode.OK, Node2Plain);

        var connections = new List<VpnConnection>
        {
            new()
            {
                ClientId = "client-plain",
                Engine = "fetch",
                SubscriptionUrl = UpstreamUrl
            }
        };

        var (service, _) = CreateService(connections, httpResponse);

        // Act
        var result = await service.GetSubscriptionAsync("client-plain");

        // Assert
        Assert.NotNull(result);
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(result.Content));
        Assert.Equal(Node2Plain, decoded);
    }

    [Fact]
    public async Task GetSubscriptionAsync_ShouldThrowHttpRequestException_WhenUpstreamReturnsError()
    {
        // Arrange: Upstream returns HTTP 500
        var httpResponse = CreateHttpResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

        var connections = new List<VpnConnection>
        {
            new()
            {
                ClientId = "client-error",
                Engine = "fetch",
                SubscriptionUrl = UpstreamUrl
            }
        };

        var (service, _) = CreateService(connections, httpResponse);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.GetSubscriptionAsync("client-error"));
    }

    [Fact]
    public async Task GetSubscriptionAsync_ShouldReturnNull_WhenUpstreamReturnsEmptyConfigs()
    {
        // Arrange: Upstream returns 200 OK with empty body
        var httpResponse = CreateHttpResponse(HttpStatusCode.OK, string.Empty);

        var connections = new List<VpnConnection>
        {
            new()
            {
                ClientId = "client-empty",
                Engine = "fetch",
                SubscriptionUrl = UpstreamUrl
            }
        };

        var (service, _) = CreateService(connections, httpResponse);

        // Act
        var result = await service.GetSubscriptionAsync("client-empty");

        // Assert
        Assert.Null(result);
    }

    private sealed class FakeDocumentReader(SubscriptionsDocument? document) : ISubscriptionDocumentReader
    {
        public Task<SubscriptionsDocument?> ReadAsync(string discoveryPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(document);
    }

    private sealed class TestHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class TestHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(response);
        }
    }
}
