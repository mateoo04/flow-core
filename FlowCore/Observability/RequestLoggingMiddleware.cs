using System.Diagnostics;
using System.Security.Claims;

namespace FlowCore.Observability;

/// <summary>Logs one structured event per dynamic request and any unhandled exception.</summary>
public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unhandled request exception. {Method} {Path} {RequestId}",
                context.Request.Method,
                context.Request.Path.Value,
                context.TraceIdentifier);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var level = context.Response.StatusCode >= StatusCodes.Status500InternalServerError
                ? LogLevel.Error
                : context.Response.StatusCode >= StatusCodes.Status400BadRequest
                    ? LogLevel.Warning
                    : LogLevel.Information;

            logger.Log(
                level,
                "HTTP request completed. {Method} {Path} {StatusCode} {DurationMs} {RequestId} {TraceId} {UserId}",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds,
                context.TraceIdentifier,
                Activity.Current?.TraceId.ToString(),
                userId);
        }
    }
}
