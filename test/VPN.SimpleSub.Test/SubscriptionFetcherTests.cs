using System.Net;
using System.Text;
using VPN.SimpleSub.Web.Services;

namespace VPN.SimpleSub.Test;

public class SubscriptionFetcherTests
{
    private const string SubUrl = "https://example.com/sub/test123";
    private const string ExpectedVless =
        "vless://00000000-0000-0000-0000-000000000000@example.com:443?type=xhttp&security=none#TestNode";

    private static IHttpClientFactory CreateMockHttpClientFactory(
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

        var handler = new MockHttpMessageHandler(response);
        return new MockHttpClientFactory(new HttpClient(handler));
    }

    [Fact]
    public async Task FetchAsync_ShouldDecodeBase64Body_AndReturnVlessConfig()
    {
        // Arrange
        var base64Body = Convert.ToBase64String(Encoding.UTF8.GetBytes(ExpectedVless));
        var factory = CreateMockHttpClientFactory(HttpStatusCode.OK, base64Body);
        var fetcher = new SubscriptionFetcher(factory);

        // Act
        var result = await fetcher.FetchAsync(SubUrl, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ExpectedVless, result.VlessConfig);
    }

    [Fact]
    public async Task FetchAsync_ShouldHandlePlainVlessBody_WhenNotBase64()
    {
        // Arrange
        var factory = CreateMockHttpClientFactory(HttpStatusCode.OK, ExpectedVless);
        var fetcher = new SubscriptionFetcher(factory);

        // Act
        var result = await fetcher.FetchAsync(SubUrl, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ExpectedVless, result.VlessConfig);
    }

    [Fact]
    public async Task FetchAsync_ShouldParseBase64ProfileTitleHeader()
    {
        // Arrange
        var base64Body = Convert.ToBase64String(Encoding.UTF8.GetBytes(ExpectedVless));
        var headers = new Dictionary<string, string>
        {
            { "Profile-Title", "base64:WFhYMw==" } // Decodes to "XXX3"
        };
        var factory = CreateMockHttpClientFactory(HttpStatusCode.OK, base64Body, headers);
        var fetcher = new SubscriptionFetcher(factory);

        // Act
        var result = await fetcher.FetchAsync(SubUrl, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("XXX3", result.ProfileTitle);
    }

    [Fact]
    public async Task FetchAsync_ShouldParsePlainProfileTitleHeader()
    {
        // Arrange
        var base64Body = Convert.ToBase64String(Encoding.UTF8.GetBytes(ExpectedVless));
        var headers = new Dictionary<string, string>
        {
            { "Profile-Title", "PlainTitle" }
        };
        var factory = CreateMockHttpClientFactory(HttpStatusCode.OK, base64Body, headers);
        var fetcher = new SubscriptionFetcher(factory);

        // Act
        var result = await fetcher.FetchAsync(SubUrl, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("PlainTitle", result.ProfileTitle);
    }

    [Fact]
    public async Task FetchAsync_ShouldParseProfileUpdateIntervalHeader()
    {
        // Arrange
        var base64Body = Convert.ToBase64String(Encoding.UTF8.GetBytes(ExpectedVless));
        var headers = new Dictionary<string, string>
        {
            { "Profile-Update-Interval", "12" }
        };
        var factory = CreateMockHttpClientFactory(HttpStatusCode.OK, base64Body, headers);
        var fetcher = new SubscriptionFetcher(factory);

        // Act
        var result = await fetcher.FetchAsync(SubUrl, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(12, result.ProfileUpdateInterval);
    }

    [Fact]
    public async Task FetchAsync_ShouldExtractSubscriptionUserInfoHeader()
    {
        // Arrange
        var base64Body = Convert.ToBase64String(Encoding.UTF8.GetBytes(ExpectedVless));
        const string expectedUserInfo = "upload=0; download=0; total=0; expire=0";
        var headers = new Dictionary<string, string>
        {
            { "Subscription-Userinfo", expectedUserInfo }
        };
        var factory = CreateMockHttpClientFactory(HttpStatusCode.OK, base64Body, headers);
        var fetcher = new SubscriptionFetcher(factory);

        // Act
        var result = await fetcher.FetchAsync(SubUrl, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedUserInfo, result.SubscriptionUserInfo);
    }

    [Fact]
    public async Task FetchAsync_ShouldThrowHttpRequestException_WhenResponseStatusIsNotSuccessful()
    {
        // Arrange
        var factory = CreateMockHttpClientFactory(HttpStatusCode.NotFound, "Not Found");
        var fetcher = new SubscriptionFetcher(factory);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            fetcher.FetchAsync(SubUrl, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FetchAsync_ShouldThrowArgumentException_WhenUrlIsInvalid(string invalidUrl)
    {
        // Arrange
        var fetcher = new SubscriptionFetcher(new MockHttpClientFactory(new HttpClient()));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            fetcher.FetchAsync(invalidUrl, TestContext.Current.CancellationToken));
    }

    private sealed class MockHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private class MockHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(response);
        }
    }
}
