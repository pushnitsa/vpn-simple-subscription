namespace VPN.SimpleSub.Web.Services;

public interface IVlessConfigEditor
{
    string SetHostAndPort(string vlessUri, string host, int? port = null);
    string SetQueryParameter(string vlessUri, string key, string value);
    string RemoveQueryParameter(string vlessUri, string key);
}
