namespace VPN.SimpleSub.Web.Services;

public static class ConnectionLineFormatter
{
    public static string Format(string connectionString, string? title)
    {
        if (string.IsNullOrWhiteSpace(title) || connectionString.Contains('#'))
        {
            return connectionString;
        }

        return $"{connectionString}#{title}";
    }
}
