using BillingInventory.Application.Abstractions;
using BillingInventory.Application.Common.Paging;
using BillingInventory.Application.Exceptions;
using BillingInventory.Domain.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingInventory.Application.Users;

/// <summary>Implementación de <see cref="IUserManagementService"/>.</summary>
public sealed class UserManagementService(
    IAppDbContext db,
    IValidator<UserQuery> queryValidator,
    IValidator<ChangeRoleRequest> roleValidator,
    ILogger<UserManagementService> logger) : IUserManagementService
{
    private static readonly SortMap<User> Sorting = new SortMap<User>("email")
        .Add("email", u => u.Email)
        .Add("fullName", u => u.FullName)
        .Add("role", u => u.Role.Name)
        .Add("createdAt", u => u.CreatedAtUtc);

    /// <inheritdoc />
    public async Task<PagedResult<UserAdminResponse>> GetPagedAsync(
        UserQuery query,
        CancellationToken cancellationToken = default)
    {
        await queryValidator.ValidateAndThrowAsync(query, cancellationToken);

        var search = query.Search?.Trim();
        var role = RoleNames.Normalize(query.Role);

        var filtered = db.Users.AsNoTracking()
            .WhereIf(!string.IsNullOrEmpty(search), u => u.Email.Contains(search!) || u.FullName.Contains(search!))
            .WhereIf(role is not null, u => u.Role.Name == role)
            .WhereIf(query.IsActive.HasValue, u => u.IsActive == query.IsActive!.Value);

        return await Sorting.Apply(filtered, query.SortBy, query.SortDirection)
            .Select(u => new UserAdminResponse
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role.Name,
                IsActive = u.IsActive,
                CreatedAtUtc = u.CreatedAtUtc,
            })
            .ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserAdminResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException("Usuario", id);

        return ToResponse(user);
    }

    /// <inheritdoc />
    public async Task<UserAdminResponse> ChangeRoleAsync(
        int id,
        ChangeRoleRequest request,
        int actingUserId,
        CancellationToken cancellationToken = default)
    {
        await roleValidator.ValidateAndThrowAsync(request, cancellationToken);

        // Un admin no puede quitarse a sí mismo el rol: si es el único, el sistema quedaría sin administradores.
        if (id == actingUserId)
        {
            throw new BusinessRuleException("No puede cambiar su propio rol.");
        }

        var user = await db.Users.Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException("Usuario", id);

        var roleName = RoleNames.Normalize(request.Role)!;
        var role = await db.Roles.SingleAsync(r => r.Name == roleName, cancellationToken);

        user.RoleId = role.Id;
        user.Role = role;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Usuario {UserId} cambió el rol de {TargetId} a {Role}", actingUserId, id, roleName);

        return ToResponse(user);
    }

    /// <inheritdoc />
    public async Task<UserAdminResponse> SetStatusAsync(
        int id,
        SetUserStatusRequest request,
        int actingUserId,
        CancellationToken cancellationToken = default)
    {
        if (id == actingUserId && !request.IsActive)
        {
            throw new BusinessRuleException("No puede desactivar su propia cuenta.");
        }

        var user = await db.Users.Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException("Usuario", id);

        user.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Usuario {UserId} puso activo={IsActive} a {TargetId}", actingUserId, request.IsActive, id);

        return ToResponse(user);
    }

    private static UserAdminResponse ToResponse(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        Role = user.Role.Name,
        IsActive = user.IsActive,
        CreatedAtUtc = user.CreatedAtUtc,
    };
}
