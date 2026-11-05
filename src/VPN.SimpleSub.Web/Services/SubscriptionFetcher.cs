using System.Text;
using VPN.SimpleSub.Web.Constants;
using VPN.SimpleSub.Web.Models;

namespace VPN.SimpleSub.Web.Services;

public class SubscriptionFetcher(IHttpClientFactory httpClientFactory) : ISubscriptionFetcher
{
    public async Task<SubscriptionResult> FetchAsync(string subscriptionUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subscriptionUrl))
        {
            throw new ArgumentException("Subscription URL cannot be null or whitespace.", nameof(subscriptionUrl));
        }

        var client = httpClientFactory.CreateClient();
        using var response = await client.GetAsync(subscriptionUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();

        var vlessConfigs = ResolveVlessConfigs(content);
        var profileTitle = ParseProfileTitle(GetHeaderValue(response, SubscriptionHeaders.Title));
        var updateInterval = ParseUpdateInterval(GetHeaderValue(response, SubscriptionHeaders.UpdateInterval));
        var userInfo = GetHeaderValue(response, SubscriptionHeaders.UserInfo);

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in response.Headers)
        {
            if (header.Key.StartsWith(SubscriptionHeaders.ProfilePrefix, StringComparison.OrdinalIgnoreCase) ||
                header.Key.StartsWith(SubscriptionHeaders.SubscriptionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }
        }

        foreach (var header in response.Content.Headers)
        {
            if (header.Key.StartsWith(SubscriptionHeaders.ProfilePrefix, StringComparison.OrdinalIgnoreCase) ||
                header.Key.StartsWith(SubscriptionHeaders.SubscriptionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }
        }

        return new SubscriptionResult(vlessConfigs, profileTitle, updateInterval, userInfo, headers);
    }

    private static IReadOnlyList<string> ResolveVlessConfigs(string content)
    {
        string rawText;

        if (content.StartsWith("vless://", StringComparison.OrdinalIgnoreCase))
        {
            rawText = content;
        }
        else
        {
            try
            {
                var bytes = Convert.FromBase64String(content);
                rawText = Encoding.UTF8.GetString(bytes).Trim();
            }
            catch (FormatException)
            {
                rawText = content;
            }
        }

        return rawText
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static string? ParseProfileTitle(string? rawTitle)
    {
        if (string.IsNullOrWhiteSpace(rawTitle))
        {
            return null;
        }

        const string base64Prefix = "base64:";
        if (rawTitle.StartsWith(base64Prefix, StringComparison.OrdinalIgnoreCase))
        {
            var base64Part = rawTitle[base64Prefix.Length..].Trim();
            try
            {
                var bytes = Convert.FromBase64String(base64Part);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                return rawTitle;
            }
        }

        return rawTitle;
    }

    private static int? ParseUpdateInterval(string? rawInterval)
    {
        if (int.TryParse(rawInterval, out var interval))
        {
            return interval;
        }

        return null;
    }

    private static string? GetHeaderValue(HttpResponseMessage response, string headerName)
    {
        if (response.Headers.TryGetValues(headerName, out var values))
        {
            return values.FirstOrDefault();
        }

        if (response.Content.Headers.TryGetValues(headerName, out var contentValues))
        {
            return contentValues.FirstOrDefault();
        }

        return null;
    }
}
