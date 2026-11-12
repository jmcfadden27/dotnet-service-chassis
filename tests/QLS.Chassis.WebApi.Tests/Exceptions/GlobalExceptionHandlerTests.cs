using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using QLS.Chassis.WebApi.Exceptions;
using QLS.Chassis.WebApi.Observability;

namespace QLS.Chassis.WebApi.Tests.Exceptions;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_MapsApiExceptionAndWritesProblemDetails()
    {
        var problemDetailsService = new RecordingProblemDetailsService();
        var requestLogger = new RecordingRequestLogger();
        var handler = new GlobalExceptionHandler(problemDetailsService, requestLogger);
        var context = new DefaultHttpContext();
        context.Request.Path = "/users";

        var handled = await handler.TryHandleAsync(
            context,
            new ApiException(StatusCodes.Status409Conflict, "Conflict", "already exists"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal(StatusCodes.Status409Conflict, problemDetailsService.LastContext!.ProblemDetails.Status);
        Assert.Equal("Conflict", problemDetailsService.LastContext.ProblemDetails.Title);
        Assert.Equal(1, requestLogger.LogErrorCalls);
    }

    [Fact]
    public async Task TryHandleAsync_MapsValidationExceptionToBadRequest()
    {
        var problemDetailsService = new RecordingProblemDetailsService();
        var handler = new GlobalExceptionHandler(problemDetailsService, new RecordingRequestLogger());
        var context = new DefaultHttpContext();

        await handler.TryHandleAsync(context, new ValidationException("invalid"), CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("Validation error", problemDetailsService.LastContext!.ProblemDetails.Title);
    }

    [Fact]
    public async Task TryHandleAsync_MapsUnknownExceptionToInternalServerError()
    {
        var problemDetailsService = new RecordingProblemDetailsService();
        var handler = new GlobalExceptionHandler(problemDetailsService, new RecordingRequestLogger());
        var context = new DefaultHttpContext();

        await handler.TryHandleAsync(context, new Exception("boom"), CancellationToken.None);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("Unexpected error", problemDetailsService.LastContext!.ProblemDetails.Title);
    }

    private sealed class RecordingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetailsContext? LastContext { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            LastContext = context;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            LastContext = context;
            return ValueTask.FromResult(true);
        }
    }

    private sealed class RecordingRequestLogger : IWebApiRequestLogger
    {
        public int LogErrorCalls { get; private set; }

        public void LogSuccess(HttpContext context)
        {
        }

        public void LogError(HttpContext context, Exception exception, int statusCode)
        {
            LogErrorCalls++;
        }
    }
}
