using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using BillingInventory.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BillingInventory.Api.Extensions;

/// <summary>Autenticación JWT y límite de intentos para los endpoints de autenticación.</summary>
public static class AuthenticationExtensions
{
    /// <summary>Nombre de la política de límite de peticiones para login y registro.</summary>
    public const string AuthRateLimitPolicy = "auth";

    /// <summary>
    /// Configura la autenticación con JWT Bearer usando <see cref="JwtOptions"/>: valida firma, emisor,
    /// audiencia y expiración, y conserva los nombres originales de los claims (<c>sub</c>, <c>role</c>).
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <returns>La misma colección, para encadenar llamadas.</returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Se configura con IOptions<JwtOptions> para leer la clave ya validada, no directamente de appsettings.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = JwtTokenGenerator.NameClaim,
                    RoleClaimType = JwtTokenGenerator.RoleClaim,
                };
            });

        return services;
    }

    /// <summary>
    /// Limita los intentos por IP en los endpoints marcados con la política <see cref="AuthRateLimitPolicy"/>
    /// para dificultar la fuerza bruta de contraseñas. Responde 429 con <c>ProblemDetails</c>.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración con <c>RateLimiting:AuthPermitPerMinute</c> (10 por defecto).</param>
    /// <returns>La misma colección, para encadenar llamadas.</returns>
    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitPerMinute = configuration.GetValue("RateLimiting:AuthPermitPerMinute", 10);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthRateLimitPolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Demasiados intentos",
                        Detail = "Se superó el límite de intentos. Espere un momento antes de reintentar.",
                        Instance = context.HttpContext.Request.Path,
                    },
                    options: null,
                    contentType: "application/problem+json",
                    cancellationToken);
            };
        });

        return services;
    }
}
