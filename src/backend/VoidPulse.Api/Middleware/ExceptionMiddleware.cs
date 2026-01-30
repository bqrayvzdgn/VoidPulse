using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VoidPulse.Application.Common;
using VoidPulse.Domain.Exceptions;

namespace VoidPulse.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment environment)
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
        var (statusCode, response) = exception switch
        {
            NotFoundException ex => (
                HttpStatusCode.NotFound,
                ApiResponse<object>.Fail("NOT_FOUND", ex.Message)),

            UnauthorizedException ex => (
                HttpStatusCode.Unauthorized,
                ApiResponse<object>.Fail("UNAUTHORIZED", ex.Message)),

            DomainException ex => (
                MapDomainExceptionStatusCode(ex),
                ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)),

            ValidationException ex => (
                HttpStatusCode.BadRequest,
                ApiResponse<object>.Fail(
                    "VALIDATION_ERROR",
                    "Validation failed",
                    ex.Errors.Select(e => new FieldError
                    {
                        Field = e.PropertyName,
                        Message = e.ErrorMessage
                    }).ToList())),

            DbUpdateException ex => (
                HttpStatusCode.Conflict,
                ApiResponse<object>.Fail("CONFLICT", "A record with the same unique values already exists.")),

            _ => HandleUnexpectedException(exception)
        };

        _logger.LogError(exception, "An error occurred processing the request. StatusCode: {StatusCode}", (int)statusCode);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);

        await context.Response.WriteAsync(json);
    }

    private static HttpStatusCode MapDomainExceptionStatusCode(DomainException exception)
    {
        return exception switch
        {
            NotFoundException => HttpStatusCode.NotFound,
            UnauthorizedException => HttpStatusCode.Unauthorized,
            _ => HttpStatusCode.BadRequest
        };
    }

    private (HttpStatusCode, ApiResponse<object>) HandleUnexpectedException(Exception exception)
    {
        var message = _environment.IsDevelopment()
            ? exception.ToString()
            : "An unexpected error occurred. Please try again later.";

        return (HttpStatusCode.InternalServerError, ApiResponse<object>.Fail("INTERNAL_ERROR", message));
    }
}
