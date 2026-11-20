# Changelog

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/).

## [No publicado]

### Añadido
- Bloque 1.1: estructura de la solución en capas, `Directory.Build.props` (nullable, XML docs y warnings como errores), `appsettings.Example.json`, README inicial y ADR-001.

- Bloque 1.2: `BaseEntity` (Id, auditoría, `RowVersion`), `IAppDbContext`, `AppDbContext` con precisión `decimal(18,2)`, registro de DI, migración `InitialCreate`, fixture de pruebas con SQL Server en Testcontainers y ADR-002.

- Bloque 1.3: Serilog (consola y archivo JSON), `CorrelationIdMiddleware`, `GlobalExceptionHandler` con `ProblemDetails`, excepciones tipadas (`NotFoundException`, `ConflictException`, `BusinessRuleException`), pruebas del pipeline de errores y ADR-003.

- Bloque 1.4: Swagger/OpenAPI (Swashbuckle) con comentarios XML y esquema Bearer, solo en Development; endpoints `/health` y `/health/ready`; pruebas de integración.

- Bloque 1.5: paginación, filtros y ordenamiento compartidos (`PagedQuery`, `PagedResult<T>`, `SortMap<T>`, `WhereIf`, `ToPagedResultAsync`), registro de validadores de Application, enumeraciones como texto en JSON, pruebas unitarias y de integración con SQL Server, y ADR-004.

- Bloque 2.1: entidades `User` y `Role` con roles sembrados (`Admin`, `Vendedor`, `Almacen`), `IPasswordHasher` con PBKDF2 de Identity, migración `AddUsersAndRoles`, pruebas de hash, auditoría, índice único y concurrencia, y ADR-005.

- Bloque 2.2: registro, login y `GET /api/auth/me` con JWT Bearer; `AuthService`, `JwtTokenGenerator`, validación de `JwtOptions` al arrancar, límite de intentos por IP, respuesta 401 uniforme con `WWW-Authenticate`, filtro de Swagger que marca solo los endpoints protegidos, pruebas unitarias y de integración, y ADR-006.

- Bloque 2.3: políticas de autorización por rol (`Policies`), política por defecto que exige autenticación, y administración de usuarios para el Admin (`/api/users`: listar con filtros y paginación, detalle, cambio de rol y activar/desactivar) con protección contra auto-bloqueo; pruebas de la matriz rol × política y de los endpoints; ADR-007.

- Bloque 2.4: administrador inicial desde user-secrets (`Seed:AdminEmail`, `Seed:AdminPassword`) mediante `AdminSeeder` y un servicio de arranque; idempotente, sin ascender cuentas existentes y sin consultar la base si no hay configuración; pruebas de reglas y de arranque de la API; ADR-008.

### Cambiado
- Una ruta inexistente consultada sin token responde 401 en lugar de 404 (política por defecto).
- `appsettings*.json`: la sección `Logging` se reemplaza por `Serilog`.

### Eliminado
- Archivos de plantilla (`WeatherForecast`, `Class1`, `UnitTest1`).
