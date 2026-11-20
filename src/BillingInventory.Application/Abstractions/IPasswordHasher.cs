namespace BillingInventory.Application.Abstractions;

/// <summary>Resultado de comprobar una contraseña contra un hash.</summary>
public enum PasswordVerificationResult
{
    /// <summary>La contraseña no coincide.</summary>
    Failed,

    /// <summary>La contraseña coincide.</summary>
    Success,

    /// <summary>La contraseña coincide, pero el hash usa parámetros antiguos y conviene regenerarlo.</summary>
    SuccessRehashNeeded,
}

/// <summary>Hash seguro de contraseñas. La implementación no debe permitir recuperar la contraseña original.</summary>
public interface IPasswordHasher
{
    /// <summary>Genera el hash de una contraseña con sal aleatoria.</summary>
    /// <param name="password">Contraseña en texto plano.</param>
    /// <returns>Hash listo para guardar.</returns>
    string Hash(string password);

    /// <summary>Comprueba una contraseña contra un hash guardado.</summary>
    /// <param name="hash">Hash guardado.</param>
    /// <param name="password">Contraseña recibida.</param>
    /// <returns>El resultado de la comprobación.</returns>
    PasswordVerificationResult Verify(string hash, string password);
}
