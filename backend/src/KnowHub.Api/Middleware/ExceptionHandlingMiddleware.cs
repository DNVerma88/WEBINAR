using KnowHub.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace KnowHub.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail, errors) = exception switch
        {
            NotFoundException notFound => (StatusCodes.Status404NotFound, "Not Found", notFound.Message, (IDictionary<string, string[]>?)null),
            ConflictException conflict => (StatusCodes.Status409Conflict, "Conflict", conflict.Message, null),
            // B25: ForbiddenException messages may expose internal state-machine details; return a generic message
            ForbiddenException _ => (StatusCodes.Status403Forbidden, "Forbidden", "You do not have permission to perform this action.", null),
            BusinessRuleException business => (StatusCodes.Status422UnprocessableEntity, "Business Rule Violation", business.Message, null),
            Domain.Exceptions.ValidationException validation => (StatusCodes.Status400BadRequest, "Validation Failed", "One or more validation errors occurred.", (IDictionary<string, string[]>?)validation.Errors),
            UnauthorizedAccessException _ => (StatusCodes.Status401Unauthorized, "Unauthorized", "Authentication is required.", null),
            _ => LogAndReturnInternalError(exception)
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problem = new ProblemDetails
        {
            Type = GetRfcType(statusCode),
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };

        if (errors is not null)
            problem.Extensions["errors"] = errors;

        problem.Extensions["traceId"] = context.TraceIdentifier;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    private (int, string, string, IDictionary<string, string[]>?) LogAndReturnInternalError(Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
        return (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.", null);
    }

    private static string GetRfcType(int statusCode) => statusCode switch
    {
        400 => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        401 => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        403 => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        404 => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        409 => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        422 => "https://tools.ietf.org/html/rfc9110#section-15.5.21",
        _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1"
    };
}
