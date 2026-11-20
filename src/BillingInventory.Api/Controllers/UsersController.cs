using BillingInventory.Api.Authorization;
using BillingInventory.Api.Extensions;
using BillingInventory.Application.Common.Paging;
using BillingInventory.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillingInventory.Api.Controllers;

/// <summary>Administración de usuarios. Solo para el rol Admin.</summary>
/// <param name="users">Servicio de administración de usuarios.</param>
[ApiController]
[Route("api/users")]
[Authorize(Policy = Policies.AdminOnly)]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class UsersController(IUserManagementService users) : ControllerBase
{
    /// <summary>Lista usuarios con filtros, orden y paginación.</summary>
    /// <remarks>
    /// Campos de orden permitidos (`sortBy`): `email` (por defecto), `fullName`, `role`, `createdAt`.
    ///
    /// Ejemplo: `GET /api/users?role=Vendedor&amp;isActive=true&amp;sortBy=createdAt&amp;sortDirection=desc&amp;page=1&amp;pageSize=10`
    /// </remarks>
    /// <param name="query">Filtros y parámetros de página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Una página de usuarios con metadatos.</returns>
    /// <response code="200">Página de usuarios.</response>
    /// <response code="400">Parámetros inválidos (página, tamaño, rol o campo de orden).</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<UserAdminResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] UserQuery query, CancellationToken cancellationToken) =>
        Ok(await users.GetPagedAsync(query, cancellationToken));

    /// <summary>Obtiene un usuario por identificador.</summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El usuario.</returns>
    /// <response code="200">Usuario encontrado.</response>
    /// <response code="404">No existe el usuario.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<UserAdminResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await users.GetByIdAsync(id, cancellationToken));

    /// <summary>Cambia el rol de un usuario.</summary>
    /// <remarks>
    /// El cambio aplica al siguiente inicio de sesión del usuario: el token ya emitido conserva el rol anterior
    /// hasta que expire. No se puede cambiar el propio rol.
    ///
    ///     PUT /api/users/7/role
    ///     { "role": "Almacen" }
    /// </remarks>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="request">Nuevo rol.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El usuario actualizado.</returns>
    /// <response code="200">Rol actualizado.</response>
    /// <response code="400">Rol no válido.</response>
    /// <response code="404">No existe el usuario.</response>
    /// <response code="422">Intento de cambiar el propio rol.</response>
    [HttpPut("{id:int}/role")]
    [ProducesResponseType<UserAdminResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeRole(
        int id,
        [FromBody] ChangeRoleRequest request,
        CancellationToken cancellationToken) =>
        Ok(await users.ChangeRoleAsync(id, request, User.GetUserId(), cancellationToken));

    /// <summary>Activa o desactiva un usuario.</summary>
    /// <remarks>
    /// Un usuario inactivo no puede iniciar sesión (su token ya emitido sigue válido hasta expirar).
    /// No se puede desactivar la propia cuenta.
    ///
    ///     PUT /api/users/7/status
    ///     { "isActive": false }
    /// </remarks>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="request">Nuevo estado.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El usuario actualizado.</returns>
    /// <response code="200">Estado actualizado.</response>
    /// <response code="404">No existe el usuario.</response>
    /// <response code="422">Intento de desactivar la propia cuenta.</response>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType<UserAdminResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SetStatus(
        int id,
        [FromBody] SetUserStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await users.SetStatusAsync(id, request, User.GetUserId(), cancellationToken));
}
