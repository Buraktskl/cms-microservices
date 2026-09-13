using System.Net;
using System.Text.Json;
using UserService.Application.Exceptions;

namespace UserService.API.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            UserNotFoundException => (HttpStatusCode.NotFound, "Not Found"),
            UserAlreadyExistsException => (HttpStatusCode.Conflict, "Conflict"),
            UserDeletionFailedException => (HttpStatusCode.InternalServerError, "Deletion Failed"),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error")
        };

        var correlationId = context.Items["CorrelationId"]?.ToString();

        var response = new
        {
            type = $"https://tools.ietf.org/html/rfc7231#section-6.{(int)statusCode}",
            title,
            status = (int)statusCode,
            detail = exception.Message,
            correlationId
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
