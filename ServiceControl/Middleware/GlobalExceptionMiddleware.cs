using ServiceControl.Application.Exceptions;
using ServiceControl.Domain.Exceptions;
using ServiceControl.Models.Responses;
using System.Net;
using System.Text.Json;

namespace ServiceControl.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        _logger.LogError(exception, "An unhandled exception occurred");

        context.Response.ContentType = "application/json";

        var (statusCode, response) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                new ApiResponse<object>(
                    false,
                    Message: "Erro de validação",
                    Errors: validationEx.Failures.Select(f => f.ErrorMessage))),

            DomainException domainEx => (
                HttpStatusCode.BadRequest,
                new ApiResponse<object>(
                    false,
                    Message: domainEx.Message)),

            WeatherServiceException weatherEx => (
                HttpStatusCode.ServiceUnavailable,
                new ApiResponse<object>(
                    false,
                    Message: "Serviço meteorológico temporariamente indisponível")),

            OperationCanceledException => (
                HttpStatusCode.RequestTimeout,
                new ApiResponse<object>(
                    false,
                    Message: "Operação cancelada por timeout")),

            _ => (
                HttpStatusCode.InternalServerError,
                new ApiResponse<object>(
                    false,
                    Message: "Erro interno do servidor"))
        };

        context.Response.StatusCode = (int)statusCode;

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}