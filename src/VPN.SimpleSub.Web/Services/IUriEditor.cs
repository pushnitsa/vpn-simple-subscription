namespace VPN.SimpleSub.Web.Services;

public interface IUriEditor
{
    string SetHostAndPort(string uri, string host, int? port = null);
    string SetQueryParameter(string uri, string key, string value);
    string RemoveQueryParameter(string uri, string key);
}
