using BillingInventory.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BillingInventory.Application.Abstractions;

/// <summary>
/// Abstracción del contexto de base de datos que usan los servicios de aplicación.
/// Evita que Application dependa de la implementación concreta de Infrastructure.
/// </summary>
public interface IAppDbContext
{
    /// <summary>Acceso a operaciones de base de datos como transacciones explícitas.</summary>
    DatabaseFacade Database { get; }

    /// <summary>Usuarios del sistema.</summary>
    DbSet<User> Users { get; }

    /// <summary>Roles del sistema.</summary>
    DbSet<Role> Roles { get; }

    /// <summary>Guarda los cambios pendientes en la base de datos.</summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Número de filas afectadas.</returns>
    /// <exception cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException">
    /// Otra operación modificó la misma fila (control optimista con <c>RowVersion</c>).
    /// </exception>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
