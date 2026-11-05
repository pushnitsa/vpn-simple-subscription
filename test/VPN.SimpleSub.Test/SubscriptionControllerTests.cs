using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using VPN.SimpleSub.Web.Constants;
using VPN.SimpleSub.Web.Controllers;
using VPN.SimpleSub.Web.Models;
using VPN.SimpleSub.Web.Services;

namespace VPN.SimpleSub.Test;

public class SubscriptionControllerTests
{
    [Fact]
    public async Task Index_ShouldReturnOkWithHeaders_WhenSubscriptionExists()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            [SubscriptionHeaders.UserInfo] = "upload=100; download=200; total=1000; expire=0",
            [SubscriptionHeaders.UpdateInterval] = "12",
            [SubscriptionHeaders.Title] = "base64:VGVzdA=="
        };
        var subResponse = new SubscriptionResponse("dGVzdC1jb250ZW50", headers);

        var fakeService = new FakeSubscriptionService(subResponse);
        var controller = new SubscriptionController(fakeService, NullLogger<SubscriptionController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // Act
        var actionResult = await controller.Index("client1");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("dGVzdC1jb250ZW50", okResult.Value);

        var responseHeaders = controller.Response.Headers;
        Assert.Equal("upload=100; download=200; total=1000; expire=0", responseHeaders[SubscriptionHeaders.UserInfo].ToString());
        Assert.Equal("12", responseHeaders[SubscriptionHeaders.UpdateInterval].ToString());
        Assert.Equal("base64:VGVzdA==", responseHeaders[SubscriptionHeaders.Title].ToString());
    }

    [Fact]
    public async Task Index_ShouldReturnNotFound_WhenSubscriptionNotFound()
    {
        // Arrange
        var fakeService = new FakeSubscriptionService(null);
        var controller = new SubscriptionController(fakeService, NullLogger<SubscriptionController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // Act
        var actionResult = await controller.Index("unknown-client");

        // Assert
        Assert.IsType<NotFoundResult>(actionResult);
    }

    private class FakeSubscriptionService(SubscriptionResponse? response) : ISubscriptionService
    {
        public Task<SubscriptionResponse?> GetSubscriptionAsync(string clientId) =>
            Task.FromResult(response);
    }
}
