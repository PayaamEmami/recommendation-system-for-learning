using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace Rsl.Api.Middleware;

/// <summary>
/// Global exception handling middleware that converts exceptions to problem details responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred");

        var statusCode = exception switch
        {
            ArgumentException => HttpStatusCode.BadRequest,
            KeyNotFoundException => HttpStatusCode.NotFound,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            _ => HttpStatusCode.InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{(int)statusCode}",
            Title = GetTitle(statusCode),
            Status = (int)statusCode,
            Detail = _environment.IsDevelopment() ? exception.Message : "An error occurred processing your request.",
            Instance = context.Request.Path
        };

        // Add stack trace in development
        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static string GetTitle(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "Bad Request",
            HttpStatusCode.NotFound => "Not Found",
            HttpStatusCode.Unauthorized => "Unauthorized",
            HttpStatusCode.InternalServerError => "Internal Server Error",
            _ => "An error occurred"
        };
    }
}

