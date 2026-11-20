namespace BillingInventory.Application.Exceptions;

/// <summary>Se lanza cuando un recurso solicitado no existe. Se traduce a HTTP 404.</summary>
public sealed class NotFoundException : Exception
{
    /// <summary>Crea la excepción para un recurso identificado por clave.</summary>
    /// <param name="resource">Nombre del recurso (por ejemplo, "Producto").</param>
    /// <param name="key">Clave buscada.</param>
    public NotFoundException(string resource, object key)
        : base($"{resource} '{key}' no fue encontrado.")
    {
    }

    /// <summary>Crea la excepción con un mensaje propio.</summary>
    /// <param name="message">Mensaje para el cliente de la API.</param>
    public NotFoundException(string message)
        : base(message)
    {
    }
}
