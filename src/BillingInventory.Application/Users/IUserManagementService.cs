using BillingInventory.Application.Common.Paging;
using BillingInventory.Application.Exceptions;
using FluentValidation;

namespace BillingInventory.Application.Users;

/// <summary>Administración de usuarios: consulta, cambio de rol y activación. Solo para administradores.</summary>
public interface IUserManagementService
{
    /// <summary>Lista usuarios con filtros, orden y paginación.</summary>
    /// <param name="query">Filtros y parámetros de página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Una página de usuarios.</returns>
    /// <exception cref="ValidationException">Parámetros inválidos o campo de orden no permitido.</exception>
    Task<PagedResult<UserAdminResponse>> GetPagedAsync(UserQuery query, CancellationToken cancellationToken = default);

    /// <summary>Obtiene un usuario por identificador.</summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El usuario.</returns>
    /// <exception cref="NotFoundException">No existe el usuario.</exception>
    Task<UserAdminResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Cambia el rol de un usuario.</summary>
    /// <param name="id">Usuario a modificar.</param>
    /// <param name="request">Nuevo rol.</param>
    /// <param name="actingUserId">Administrador que realiza el cambio.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El usuario actualizado.</returns>
    /// <exception cref="ValidationException">Rol no válido.</exception>
    /// <exception cref="NotFoundException">No existe el usuario.</exception>
    /// <exception cref="BusinessRuleException">El administrador intenta cambiar su propio rol.</exception>
    Task<UserAdminResponse> ChangeRoleAsync(
        int id,
        ChangeRoleRequest request,
        int actingUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Activa o desactiva un usuario.</summary>
    /// <param name="id">Usuario a modificar.</param>
    /// <param name="request">Nuevo estado.</param>
    /// <param name="actingUserId">Administrador que realiza el cambio.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El usuario actualizado.</returns>
    /// <exception cref="NotFoundException">No existe el usuario.</exception>
    /// <exception cref="BusinessRuleException">El administrador intenta desactivarse a sí mismo.</exception>
    Task<UserAdminResponse> SetStatusAsync(
        int id,
        SetUserStatusRequest request,
        int actingUserId,
        CancellationToken cancellationToken = default);
}
