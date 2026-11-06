namespace VPN.SimpleSub.Web.Services;

public class VlessConfigEditor : IUriEditor
{
    public string SetHostAndPort(string uri, string? host = null, int? port = null)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException("URI cannot be empty or whitespace.", nameof(uri));
        }

        if (host != null && string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("Host cannot be empty or whitespace.", nameof(host));
        }

        var builder = new UriBuilder(uri);

        if (!string.IsNullOrWhiteSpace(host))
        {
            builder.Host = host;
        }

        if (port.HasValue)
        {
            builder.Port = port.Value;
        }

        return FormatUri(uri, builder, null);
    }

    public string SetQueryParameter(string uri, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException("URI cannot be empty or whitespace.", nameof(uri));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be empty or whitespace.", nameof(key));
        }

        var (builder, queryParams) = Parse(uri);
        var encodedValue = Uri.EscapeDataString(value);

        var existingIndex = queryParams.FindIndex(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
        {
            queryParams[existingIndex] = new KeyValuePair<string, string>(key, encodedValue);
        }
        else
        {
            queryParams.Add(new KeyValuePair<string, string>(key, encodedValue));
        }

        return FormatUri(uri, builder, queryParams);
    }

    public string RemoveQueryParameter(string uri, string key)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException("URI cannot be empty or whitespace.", nameof(uri));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be empty or whitespace.", nameof(key));
        }

        var (builder, queryParams) = Parse(uri);
        var removedCount = queryParams.RemoveAll(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));

        if (removedCount == 0)
        {
            return uri;
        }

        return FormatUri(uri, builder, queryParams);
    }

    public IReadOnlyDictionary<string, string> GetQueryParameters(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException("URI cannot be empty or whitespace.", nameof(uri));
        }

        var (_, queryParams) = Parse(uri);
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in queryParams)
        {
            dict[pair.Key] = Uri.UnescapeDataString(pair.Value);
        }

        return dict;
    }

    private static (UriBuilder builder, List<KeyValuePair<string, string>> queryParams) Parse(string uri)
    {
        var builder = new UriBuilder(uri);
        var rawQuery = builder.Query;
        var list = new List<KeyValuePair<string, string>>();

        if (!string.IsNullOrEmpty(rawQuery))
        {
            var trimmed = rawQuery.StartsWith('?') ? rawQuery[1..] : rawQuery;
            var parts = trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var eqIndex = part.IndexOf('=');
                if (eqIndex >= 0)
                {
                    var k = part[..eqIndex];
                    var v = part[(eqIndex + 1)..];
                    list.Add(new KeyValuePair<string, string>(k, v));
                }
                else
                {
                    list.Add(new KeyValuePair<string, string>(part, string.Empty));
                }
            }
        }

        return (builder, list);
    }

    private static string FormatUri(string originalUri, UriBuilder builder, List<KeyValuePair<string, string>>? queryParams)
    {
        if (queryParams != null)
        {
            if (queryParams.Count == 0)
            {
                builder.Query = string.Empty;
            }
            else
            {
                builder.Query = string.Join("&", queryParams.Select(p =>
                    string.IsNullOrEmpty(p.Value) ? p.Key : $"{p.Key}={p.Value}"));
            }
        }

        var result = builder.Uri.ToString();

        if (!originalUri.Contains("/?") && result.Contains("/?"))
        {
            result = result.Replace("/?", "?");
        }
        else if (!originalUri.Contains("/#") && result.Contains("/#") && !result.Contains('?'))
        {
            result = result.Replace("/#", "#");
        }

        return result;
    }
}
