using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BillingInventory.Infrastructure.Security;

/// <summary>Configuración de los tokens JWT (sección <c>Jwt</c>). La clave vive en user-secrets o variables de entorno.</summary>
public sealed class JwtOptions : IValidatableObject
{
    /// <summary>Nombre de la sección de configuración.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Tamaño mínimo de la clave HMAC-SHA256, en bytes.</summary>
    public const int MinKeyBytes = 32;

    /// <summary>Emisor del token (claim <c>iss</c>).</summary>
    [Required]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Audiencia del token (claim <c>aud</c>).</summary>
    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>Clave secreta de firma.</summary>
    [Required]
    public string Key { get; set; } = string.Empty;

    /// <summary>Minutos de vigencia del token.</summary>
    [Range(1, 1440)]
    public int ExpirationMinutes { get; set; } = 60;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Encoding.UTF8.GetByteCount(Key) < MinKeyBytes)
        {
            yield return new ValidationResult(
                $"Jwt:Key debe tener al menos {MinKeyBytes} bytes (256 bits) para HMAC-SHA256.",
                [nameof(Key)]);
        }
    }
}
