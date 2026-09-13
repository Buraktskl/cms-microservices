using System.Net;
using System.Text.Json;
using ContentService.Application.Exceptions;

namespace ContentService.API.Middleware;

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
            ContentNotFoundException => (HttpStatusCode.NotFound, "Not Found"),
            AuthorNotFoundException => (HttpStatusCode.UnprocessableEntity, "Author Not Found"),
            UserServiceUnavailableException => (HttpStatusCode.ServiceUnavailable, "User Service Unavailable"),
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
