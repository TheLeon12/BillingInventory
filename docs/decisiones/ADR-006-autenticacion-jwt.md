# ADR-006: Autenticación con JWT

- **Estado:** Aceptada
- **Fecha:** 2026-11-20

## Contexto

La API es sin estado y la consumirá un frontend o herramientas externas. Se necesita autenticar con correo y contraseña, identificar el rol en cada petición y resistir los ataques más comunes contra un login.

## Decisión

- **Token de acceso JWT** firmado con HMAC-SHA256, emitido por `JwtTokenGenerator` (`Microsoft.IdentityModel.JsonWebTokens`). Claims: `sub` (id), `email`, `name`, `role` y `jti`. No contiene datos sensibles.
- **Vigencia** configurable (`Jwt:ExpirationMinutes`, 60 por defecto). Sin refresh tokens: queda fuera de alcance y se documenta como mejora futura.
- **Validación en cada petición:** firma, emisor, audiencia y expiración, con tolerancia de reloj de 30 s y algoritmo restringido a HS256. `MapInboundClaims = false` conserva los nombres originales (`role`, `sub`).
- **Configuración segura:** `Jwt:Key` solo en user-secrets o variables de entorno. `JwtOptions` se valida al arrancar (`ValidateOnStart`): si falta la clave o tiene menos de 32 bytes, la aplicación no inicia.
- **Registro:** siempre crea el rol Vendedor; el cliente no puede elegir su rol (cualquier campo `role` enviado se ignora). El correo se normaliza (recortado y en minúsculas) y los duplicados concurrentes se resuelven con el índice único.
- **Login sin filtrar información:** correo inexistente, contraseña incorrecta y usuario inactivo devuelven la misma respuesta 401. Para el correo inexistente se ejecuta igualmente una verificación de hash, de modo que el tiempo de respuesta no delate si el usuario existe. Si el hash guardado usa parámetros antiguos, se regenera tras un login correcto.
- **Fuerza bruta:** `RateLimiter` de ASP.NET Core, ventana fija por IP (10 intentos por minuto por defecto, `RateLimiting:AuthPermitPerMinute`) en login y registro; responde 429 con `ProblemDetails` y `Retry-After`.
- **Contraseñas:** 8 a 128 caracteres con mayúscula, minúscula y número. El máximo evita hashear textos enormes.
- **Errores de autenticación** (401) siguen el mismo formato `ProblemDetails` y la cabecera `WWW-Authenticate: Bearer`.

## Alternativas descartadas

- Sesiones con cookie: acopladas al navegador y con estado en el servidor.
- Clave asimétrica (RS256): útil con varios emisores o verificadores; innecesaria con una sola API.
- Refresh tokens y revocación: aportan seguridad adicional pero también complejidad (tabla, rotación); se dejan como mejora.

## Consecuencias

- Un token robado sirve hasta que expira; por eso la vigencia es corta y configurable.
- Desactivar un usuario (`IsActive = false`) impide nuevos logins, pero un token ya emitido sigue siendo válido hasta su expiración.
- Detrás de un proxy inverso, el límite por IP requiere configurar cabeceras reenviadas (`X-Forwarded-For`); queda anotado en el README.
