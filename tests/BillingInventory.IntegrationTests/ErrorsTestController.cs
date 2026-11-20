using BillingInventory.Application.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillingInventory.IntegrationTests;

/// <summary>Endpoints solo para pruebas: cada uno lanza un tipo de excepción.</summary>
[ApiController]
[AllowAnonymous]
[Route("test-errors")]
public sealed class ErrorsTestController : ControllerBase
{
    [HttpGet("not-found")]
    public IActionResult NotFoundCase() => throw new NotFoundException("Producto", 42);

    [HttpGet("conflict")]
    public IActionResult ConflictCase() => throw new ConflictException("El SKU ya existe.");

    [HttpGet("rule")]
    public IActionResult RuleCase() => throw new BusinessRuleException("Stock insuficiente.");

    [HttpGet("concurrency")]
    public IActionResult ConcurrencyCase() => throw new DbUpdateConcurrencyException("secreto interno");

    [HttpGet("validation")]
    public IActionResult ValidationCase() => throw new ValidationException(
    [
        new ValidationFailure("Name", "El nombre es obligatorio."),
        new ValidationFailure("Price", "El precio debe ser mayor que cero."),
    ]);

    [HttpGet("crash")]
    public IActionResult CrashCase() => throw new InvalidOperationException("detalle interno que no debe filtrarse");
}
