namespace VPN.SimpleSub.Web.Services;

public static class ConnectionLineFormatter
{
    public static string Format(string connectionString, string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return connectionString;
        }

        var hashIndex = connectionString.IndexOf('#');
        var baseUri = hashIndex >= 0 ? connectionString[..hashIndex] : connectionString;

        return $"{baseUri}#{title}";
    }
}
