using Microsoft.OpenApi.Models;
using QLS.Chassis.WebApi.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace QLS.Chassis.WebApi.Tests.Extensions;

public sealed class SwaggerGenOptionsExtensionsTests
{
    [Fact]
    public void AddChassisJwtSecurity_AddsBearerSchemeAndAuthorizeFilter()
    {
        var options = new SwaggerGenOptions();

        options.AddChassisJwtSecurity();

        Assert.True(options.SwaggerGeneratorOptions.SecuritySchemes.ContainsKey("Bearer"));
        var scheme = options.SwaggerGeneratorOptions.SecuritySchemes["Bearer"];
        Assert.Equal(SecuritySchemeType.Http, scheme.Type);
        Assert.Equal("bearer", scheme.Scheme);

        Assert.Contains(options.OperationFilterDescriptors, descriptor =>
            descriptor.Type.Name.Contains("AuthorizeOperationFilter", StringComparison.Ordinal));
    }
}
