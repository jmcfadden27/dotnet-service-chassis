using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using QLS.Chassis.WebApi.Auth;

namespace QLS.Chassis.WebApi.Tests.Auth;

public sealed class CurrentUserAccessorTests
{
    [Fact]
    public void IsAuthenticated_ReturnsFalse_WhenNoUser()
    {
        var accessor = new CurrentUserAccessor(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });

        Assert.False(accessor.IsAuthenticated);
    }

    [Fact]
    public void Subject_UsesNameIdentifierFallback()
    {
        var expected = Guid.NewGuid().ToString();
        var accessor = CreateAccessor([new Claim(ClaimTypes.NameIdentifier, expected)]);

        Assert.Equal(expected, accessor.Subject);
    }

    [Fact]
    public void Phone_UsesMobilePhoneThenHomePhone()
    {
        var accessor = CreateAccessor(
        [
            new Claim(ClaimTypes.MobilePhone, "111-222-3333"),
            new Claim(ClaimTypes.HomePhone, "999-888-7777")
        ]);

        Assert.Equal("111-222-3333", accessor.Phone);
    }

    private static CurrentUserAccessor CreateAccessor(IEnumerable<Claim> claims)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };

        return new CurrentUserAccessor(new HttpContextAccessor { HttpContext = context });
    }
}
