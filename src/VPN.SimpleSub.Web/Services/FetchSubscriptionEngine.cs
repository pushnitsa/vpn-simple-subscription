using VPN.SimpleSub.Web.Constants;
using VPN.SimpleSub.Web.Models;

namespace VPN.SimpleSub.Web.Services;

public class FetchSubscriptionEngine(
    ISubscriptionFetcher subscriptionFetcher,
    IUriEditor uriEditor) : ISubscriptionEngine
{
    public bool CanHandle(string? engine) =>
        string.Equals(engine, "fetch", StringComparison.OrdinalIgnoreCase);

    public async Task<EngineSubscriptionResult> ResolveConfigsAsync(VpnConnection connection, CancellationToken cancellationToken = default)
    {
        var result = new List<string>();

        if (string.IsNullOrWhiteSpace(connection.SubscriptionUrl))
        {
            return new EngineSubscriptionResult(result);
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

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (fetchResult.Headers != null)
        {
            foreach (var (key, value) in fetchResult.Headers)
            {
                headers[key] = value;
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(fetchResult.SubscriptionUserInfo))
            {
                headers[SubscriptionHeaders.UserInfo] = fetchResult.SubscriptionUserInfo;
            }

            if (fetchResult.ProfileUpdateInterval.HasValue)
            {
                headers[SubscriptionHeaders.UpdateInterval] = fetchResult.ProfileUpdateInterval.Value.ToString();
            }

            if (!string.IsNullOrWhiteSpace(fetchResult.ProfileTitle))
            {
                headers[SubscriptionHeaders.Title] = fetchResult.ProfileTitle;
            }
        }

        if (!headers.ContainsKey(SubscriptionHeaders.UpdateInterval))
        {
            headers[SubscriptionHeaders.UpdateInterval] = SubscriptionHeaders.DefaultUpdateInterval;
        }

        return new EngineSubscriptionResult(result, headers);
    }

    private string ApplyOverrides(string uri, List<VpnConnectionOverride>? overrides, string? defaultTitle)
    {
        if (overrides == null || overrides.Count == 0)
        {
            return uri.Contains('#') ? uri : ConnectionLineFormatter.Format(uri, defaultTitle);
        }

        var queryParams = uriEditor.GetQueryParameters(uri);
        var sni = queryParams.GetValueOrDefault("sni");
        var httpHost = queryParams.GetValueOrDefault("host");
        var serverHost = new Uri(uri).Host;

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
}
