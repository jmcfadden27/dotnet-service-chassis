using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using QLS.Chassis.WebApi.Auth;

namespace QLS.Chassis.WebApi.Tests.Auth;

public sealed class UserContextServiceTests
{
    [Fact]
    public void GetUserId_ReturnsGuid_FromSubClaim()
    {
        var userId = Guid.NewGuid();
        var service = CreateService(true, [new Claim("sub", userId.ToString())]);

        var result = service.GetUserId();

        Assert.Equal(userId, result);
    }

    [Fact]
    public void GetUserId_ReturnsNull_WhenUnauthenticated()
    {
        var service = CreateService(false, [new Claim("sub", Guid.NewGuid().ToString())]);

        var result = service.GetUserId();

        Assert.Null(result);
    }

    [Fact]
    public void GetRequiredUserId_Throws_WhenInvalidGuid()
    {
        var service = CreateService(true, [new Claim(ClaimTypes.NameIdentifier, "not-a-guid")]);

        var ex = Assert.Throws<UserContextUnavailableException>(() => service.GetRequiredUserId());

        Assert.Equal("Authenticated user identifier is invalid.", ex.Message);
    }

    [Fact]
    public void GetRequiredUserId_UsesFallbackUsernameClaim()
    {
        var userId = Guid.NewGuid();
        var service = CreateService(true, [new Claim("username", userId.ToString())]);

        var result = service.GetRequiredUserId();

        Assert.Equal(userId, result);
    }

    private static UserContextService CreateService(bool isAuthenticated, IEnumerable<Claim> claims)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, isAuthenticated ? "test" : null))
        };

        return new UserContextService(new HttpContextAccessor { HttpContext = context });
    }
}
