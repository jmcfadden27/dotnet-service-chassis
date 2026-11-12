using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Moq;
using QLS.Chassis.WebApi.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace QLS.Chassis.WebApi.Tests.Extensions;

public sealed class SwaggerAuthorizeOperationFilterTests
{
    [Fact]
    public void Apply_AddsSecurityAnd401403_WhenAuthorized()
    {
        var filter = CreateFilter();
        var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
        var context = CreateContext(typeof(AuthorizedController).GetMethod(nameof(AuthorizedController.GetSecure))!);

        filter.Apply(operation, context);

        Assert.NotNull(operation.Security);
        Assert.NotEmpty(operation.Security);
        Assert.Contains("401", operation.Responses.Keys);
        Assert.Contains("403", operation.Responses.Keys);
    }

    [Fact]
    public void Apply_DoesNothing_WhenAllowAnonymous()
    {
        var filter = CreateFilter();
        var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
        var context = CreateContext(typeof(AuthorizedController).GetMethod(nameof(AuthorizedController.GetPublic))!);

        filter.Apply(operation, context);

        Assert.True(operation.Security == null || operation.Security.Count == 0);
        Assert.DoesNotContain("401", operation.Responses.Keys);
        Assert.DoesNotContain("403", operation.Responses.Keys);
    }

    [Fact]
    public void Apply_DoesNothing_WhenNoAuthorizeAttributes()
    {
        var filter = CreateFilter();
        var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
        var context = CreateContext(typeof(OpenController).GetMethod(nameof(OpenController.Get))!);

        filter.Apply(operation, context);

        Assert.True(operation.Security == null || operation.Security.Count == 0);
    }

    private static IOperationFilter CreateFilter()
    {
        var type = typeof(SwaggerGenOptionsExtensions).GetNestedType("AuthorizeOperationFilter", BindingFlags.NonPublic)!;
        return (IOperationFilter)Activator.CreateInstance(type)!;
    }

    private static OperationFilterContext CreateContext(MethodInfo methodInfo)
    {
        return new OperationFilterContext(
            new ApiDescription(),
            Mock.Of<ISchemaGenerator>(),
            new SchemaRepository(),
            methodInfo);
    }

    [Authorize]
    private sealed class AuthorizedController
    {
        public void GetSecure() { }

        [AllowAnonymous]
        public void GetPublic() { }
    }

    private sealed class OpenController
    {
        public void Get() { }
    }
}
