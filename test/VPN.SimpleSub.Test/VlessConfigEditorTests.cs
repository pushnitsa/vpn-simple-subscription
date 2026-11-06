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

    [Fact]
    public void SetHostAndPort_ShouldUpdatePortOnly_WhenHostIsNotSpecified()
    {
        // Act
        var result = _editor.SetHostAndPort(BaseVlessUri, port: 8443);

        // Assert
        var uri = new Uri(result);
        Assert.Equal("old.example.com", uri.Host);
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

    [Fact]
    public void GetQueryParameters_ShouldExtractAllParameters_AndUnescapeValues()
    {
        // Arrange
        const string uri = "vless://00000000-0000-0000-0000-000000000000@example.com:443?type=xhttp&security=reality&sni=sni.example.com&path=%2Fapi%2Fv1&extra=%7B%22mode%22%3A%22multi%22%7D#Node";

        // Act
        var parameters = _editor.GetQueryParameters(uri);

        // Assert
        Assert.Equal("xhttp", parameters["type"]);
        Assert.Equal("reality", parameters["security"]);
        Assert.Equal("sni.example.com", parameters["sni"]);
        Assert.Equal("/api/v1", parameters["path"]);
        Assert.Equal("{\"mode\":\"multi\"}", parameters["extra"]);
    }

    [Fact]
    public void GetQueryParameters_ShouldBeCaseInsensitive()
    {
        // Arrange
        const string uri = "vless://00000000-0000-0000-0000-000000000000@example.com:443?type=xhttp&SNI=sni.example.com#Node";

        // Act
        var parameters = _editor.GetQueryParameters(uri);

        // Assert
        Assert.Equal("xhttp", parameters["TYPE"]);
        Assert.Equal("sni.example.com", parameters["sni"]);
    }

    [Fact]
    public void GetQueryParameters_ShouldReturnEmptyDictionary_WhenUriHasNoQuery()
    {
        // Arrange
        const string uri = "vless://00000000-0000-0000-0000-000000000000@example.com:443#Node";

        // Act
        var parameters = _editor.GetQueryParameters(uri);

        // Assert
        Assert.Empty(parameters);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetQueryParameters_ShouldThrowArgumentException_WhenUriIsInvalid(string invalidUri)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _editor.GetQueryParameters(invalidUri));
    }

    [Fact]
    public void SetHostAndPort_ShouldPreserveEscapedCharactersInQuery()
    {
        // Arrange
        const string uri = "vless://00000000-0000-0000-0000-000000000000@old.example.com:443?type=xhttp&extra=%7B%22mode%22%3A%22multi%22%7D#Node 1";

        // Act
        var result = _editor.SetHostAndPort(uri, "new.example.com", 8443);

        // Assert
        Assert.StartsWith("vless://00000000-0000-0000-0000-000000000000@new.example.com:8443?type=xhttp&extra=%7B%22mode%22%3A%22multi%22%7D#Node 1", result);
        Assert.Contains("extra=%7B%22mode%22%3A%22multi%22%7D", result);
        Assert.DoesNotContain("extra={", result);
        Assert.DoesNotContain("#Node%201", result); // Fragment remains unescaped
        Assert.EndsWith("#Node 1", result);
    }

    [Fact]
    public void SetQueryParameter_ShouldPreserveOtherEscapedParameters_AndEscapeNewValue()
    {
        // Arrange
        const string uri = "vless://00000000-0000-0000-0000-000000000000@example.com:443?type=xhttp&extra=%7B%22mode%22%3A%22multi%22%7D#Node";

        // Act 1: Add new parameter, verify existing escaped param is not unescaped
        var updated = _editor.SetQueryParameter(uri, "sni", "sni.example.com");

        // Assert 1
        Assert.Contains("extra=%7B%22mode%22%3A%22multi%22%7D", updated);
        Assert.Contains("sni=sni.example.com", updated);
        Assert.DoesNotContain("extra={", updated);

        // Act 2: Overwrite extra with a JSON object, verify it gets URL-encoded
        var updatedExtra = _editor.SetQueryParameter(uri, "extra", "{\"mode\":\"packet-up\"}");

        // Assert 2
        Assert.Contains("extra=%7B%22mode%22%3A%22packet-up%22%7D", updatedExtra);
        Assert.DoesNotContain("extra={", updatedExtra);
    }

    [Fact]
    public void RemoveQueryParameter_ShouldPreserveOtherEscapedParameters()
    {
        // Arrange
        const string uri = "vless://00000000-0000-0000-0000-000000000000@example.com:443?type=xhttp&extra=%7B%22mode%22%3A%22multi%22%7D#Node";

        // Act
        var result = _editor.RemoveQueryParameter(uri, "type");

        // Assert
        Assert.Contains("extra=%7B%22mode%22%3A%22multi%22%7D", result);
        Assert.DoesNotContain("type=", result);
        Assert.DoesNotContain("extra={", result);
    }
}
