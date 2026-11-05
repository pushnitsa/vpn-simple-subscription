using VPN.SimpleSub.Web.Models;

namespace VPN.SimpleSub.Web.Services;

public class FetchSubscriptionEngine(
    ISubscriptionFetcher subscriptionFetcher,
    IUriEditor uriEditor) : ISubscriptionEngine
{
    public bool CanHandle(string? engine) =>
        string.Equals(engine, "fetch", StringComparison.OrdinalIgnoreCase);

    public async Task<List<string>> ResolveConfigsAsync(VpnConnection connection, CancellationToken cancellationToken = default)
    {
        var result = new List<string>();

        if (string.IsNullOrWhiteSpace(connection.SubscriptionUrl))
        {
            return result;
        }

        var fetchResult = await subscriptionFetcher.FetchAsync(connection.SubscriptionUrl, cancellationToken);

        foreach (var rawUri in fetchResult.VlessConfigs)
        {
            if (string.IsNullOrWhiteSpace(rawUri))
            {
                continue;
            }

            var transformedUri = ApplyOverrides(rawUri, connection.Overrides, fetchResult.ProfileTitle);
            result.Add(transformedUri);
        }

        return result;
    }

    private string ApplyOverrides(string uri, List<VpnConnectionOverride>? overrides, string? defaultTitle)
    {
        if (overrides == null || overrides.Count == 0)
        {
            return uri.Contains('#') ? uri : ConnectionLineFormatter.Format(uri, defaultTitle);
        }

        var parsed = new Uri(uri);
        var sni = GetQueryParam(parsed.Query, "sni");
        var httpHost = GetQueryParam(parsed.Query, "host");
        var serverHost = parsed.Host;

        var matchedOverride = overrides.FirstOrDefault(o =>
            Matches(o, sni, httpHost, serverHost));

        if (matchedOverride == null)
        {
            return uri.Contains('#') ? uri : ConnectionLineFormatter.Format(uri, defaultTitle);
        }

        var updatedUri = uri;

        if (!string.IsNullOrWhiteSpace(matchedOverride.Host) || matchedOverride.Port.HasValue)
        {
            updatedUri = uriEditor.SetHostAndPort(updatedUri, matchedOverride.Host, matchedOverride.Port);
        }

        var title = matchedOverride.Title ?? (updatedUri.Contains('#') ? null : defaultTitle);
        return ConnectionLineFormatter.Format(updatedUri, title);
    }

    private static bool Matches(VpnConnectionOverride o, string? sni, string? httpHost, string serverHost)
    {
        if (!string.IsNullOrWhiteSpace(o.MatchSni))
        {
            return string.Equals(o.MatchSni, sni, StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(o.MatchHost))
        {
            return string.Equals(o.MatchHost, serverHost, StringComparison.OrdinalIgnoreCase)
                || string.Equals(o.MatchHost, httpHost, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static string? GetQueryParam(string rawQuery, string key)
    {
        if (string.IsNullOrEmpty(rawQuery))
        {
            return null;
        }

        var trimmed = rawQuery.StartsWith('?') ? rawQuery[1..] : rawQuery;
        var pairs = trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            var eq = pair.IndexOf('=');
            var k = eq >= 0 ? pair[..eq] : pair;
            var v = eq >= 0 ? pair[(eq + 1)..] : string.Empty;

            if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(v);
            }
        }

        return null;
    }
}
