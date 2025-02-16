using System.Net;
using System.Text.Json;
using eCommerce.API.Middleware.Exceptions;

namespace eCommerce.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "An unhandled exception occurred while processing request {Method} {Path}. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var statusCode = ex switch
        {
            ValidationException => HttpStatusCode.BadRequest,
            ProductNotFoundException => HttpStatusCode.NotFound,
            UserNotFoundException => HttpStatusCode.NotFound,
            ArgumentException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            FirebaseAuthException => HttpStatusCode.Unauthorized,
            InvalidCredentialsException => HttpStatusCode.Unauthorized,
            UserAlreadyExistsException => HttpStatusCode.Conflict,
            TokenValidationException => HttpStatusCode.Unauthorized,
            _ => HttpStatusCode.InternalServerError
        };

        var logLevel = statusCode switch
        {
            HttpStatusCode.BadRequest => LogLevel.Information,
            HttpStatusCode.NotFound => LogLevel.Information,
            HttpStatusCode.Unauthorized => LogLevel.Warning,
            HttpStatusCode.Conflict => LogLevel.Information,
            _ => LogLevel.Error
        };

        _logger.Log(logLevel,
            ex,
            "Request {Method} {Path} failed with status {StatusCode}. Error: {ErrorType}. Message: {Message}. TraceId: {TraceId}",
            context.Request.Method,
            context.Request.Path,
            (int)statusCode,
            ex.GetType().Name,
            ex.Message,
            context.TraceIdentifier);

        var response = new
        {
            error = new
            {
                type = ex.GetType().Name,
                message = ex.Message,
                statusCode = (int)statusCode,
                errorCode = ex is FirebaseAuthException firebaseEx ? firebaseEx.ErrorCode : null
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        _logger.LogError(ex, "An error occurred: {Message}", ex.Message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}