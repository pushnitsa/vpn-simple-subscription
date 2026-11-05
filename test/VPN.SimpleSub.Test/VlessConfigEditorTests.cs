using VPN.SimpleSub.Web.Services;

namespace VPN.SimpleSub.Test;

public class VlessConfigEditorTests
{
    private readonly IUriEditor _editor = new VlessConfigEditor();

    private const string BaseVlessUri =
        "vless://00000000-0000-0000-0000-000000000000@old.example.com:443?encryption=none&security=none&type=xhttp#TestNode";

    [Fact]
    public void SetHostAndPort_ShouldUpdateHost_WhenPortIsNotSpecified()
    {
        // Act
        var result = _editor.SetHostAndPort(BaseVlessUri, "new.example.com");

        // Assert
        var uri = new Uri(result);
        Assert.Equal("new.example.com", uri.Host);
        Assert.Equal(443, uri.Port);
        Assert.Equal("00000000-0000-0000-0000-000000000000", uri.UserInfo);
        Assert.Equal("#TestNode", uri.Fragment);
        Assert.Contains("type=xhttp", uri.Query);
    }

    [Fact]
    public void SetHostAndPort_ShouldUpdateBothHostAndPort_WhenPortIsSpecified()
    {
        // Act
        var result = _editor.SetHostAndPort(BaseVlessUri, "new.example.com", 8443);

        // Assert
        var uri = new Uri(result);
        Assert.Equal("new.example.com", uri.Host);
        Assert.Equal(8443, uri.Port);
        Assert.Equal("00000000-0000-0000-0000-000000000000", uri.UserInfo);
        Assert.Equal("#TestNode", uri.Fragment);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetHostAndPort_ShouldThrowArgumentException_WhenHostIsInvalid(string invalidHost)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _editor.SetHostAndPort(BaseVlessUri, invalidHost));
    }

    [Fact]
    public void SetQueryParameter_ShouldAddNewParameter_WhenParameterDoesNotExist()
    {
        // Act
        var result = _editor.SetQueryParameter(BaseVlessUri, "sni", "custom.sni.com");

        // Assert
        var uri = new Uri(result);
        Assert.Contains("sni=custom.sni.com", uri.Query);
        Assert.Contains("type=xhttp", uri.Query);
        Assert.Equal("#TestNode", uri.Fragment);
    }

    [Fact]
    public void SetQueryParameter_ShouldUpdateExistingParameter_WhenParameterExists()
    {
        // Act
        var result = _editor.SetQueryParameter(BaseVlessUri, "security", "reality");

        // Assert
        var uri = new Uri(result);
        Assert.Contains("security=reality", uri.Query);
        Assert.DoesNotContain("security=none", uri.Query);
        Assert.Equal("#TestNode", uri.Fragment);
    }

    [Fact]
    public void SetQueryParameter_ShouldUrlEncodeValue_WhenValueContainsSpecialCharacters()
    {
        // Act
        var result = _editor.SetQueryParameter(BaseVlessUri, "path", "/api/v1?token=xyz");

        // Assert
        var uri = new Uri(result);
        Assert.Contains("path=%2Fapi%2Fv1%3Ftoken%3Dxyz", uri.Query);
    }

    [Fact]
    public void RemoveQueryParameter_ShouldRemoveParameter_WhenParameterExists()
    {
        // Act
        var result = _editor.RemoveQueryParameter(BaseVlessUri, "encryption");

        // Assert
        var uri = new Uri(result);
        Assert.DoesNotContain("encryption=", uri.Query);
        Assert.Contains("security=none", uri.Query);
        Assert.Contains("type=xhttp", uri.Query);
        Assert.Equal("#TestNode", uri.Fragment);
    }

    [Fact]
    public void RemoveQueryParameter_ShouldPreserveUri_WhenParameterDoesNotExist()
    {
        // Act
        var result = _editor.RemoveQueryParameter(BaseVlessUri, "nonexistent");

        // Assert
        var uri = new Uri(result);
        Assert.Contains("encryption=none", uri.Query);
        Assert.Contains("security=none", uri.Query);
        Assert.Contains("type=xhttp", uri.Query);
        Assert.Equal("#TestNode", uri.Fragment);
    }
}
