using BillingInventory.Application.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillingInventory.Api.Middleware;

/// <summary>
/// Traduce las excepciones no controladas a respuestas <see cref="ProblemDetails"/> (RFC 9457).
/// Los errores 5xx nunca exponen el mensaje interno de la excepción.
/// </summary>
/// <param name="problemDetailsService">Servicio que escribe la respuesta.</param>
/// <param name="logger">Logger.</param>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = Map(exception);
        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        if (problem.Status >= 500)
        {
            logger.LogError(exception, "Excepción no controlada procesando {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            // Errores esperados del cliente: sin stack trace para no llenar los logs.
            logger.LogWarning("{ExceptionType}: {Message}", exception.GetType().Name, exception.Message);
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem,
        });
    }

    private static ProblemDetails Map(Exception exception) => exception switch
    {
        ValidationException validation => new HttpValidationProblemDetails(
            validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Error de validación",
            Detail = "Uno o más campos no son válidos.",
        },
        NotFoundException notFound => Create(StatusCodes.Status404NotFound, "Recurso no encontrado", notFound.Message),
        ConflictException conflict => Create(StatusCodes.Status409Conflict, "Conflicto", conflict.Message),
        DbUpdateConcurrencyException => Create(
            StatusCodes.Status409Conflict,
            "Conflicto de concurrencia",
            "El recurso fue modificado por otra operación. Vuelva a consultarlo e intente de nuevo."),
        BusinessRuleException rule => Create(StatusCodes.Status422UnprocessableEntity, "Regla de negocio incumplida", rule.Message),
        _ => Create(
            StatusCodes.Status500InternalServerError,
            "Error interno del servidor",
            "Ocurrió un error inesperado. Indique el correlationId al reportarlo."),
    };

    private static ProblemDetails Create(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail };
}
