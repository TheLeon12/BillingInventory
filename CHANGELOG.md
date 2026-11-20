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

### Cambiado
- `appsettings*.json`: la sección `Logging` se reemplaza por `Serilog`.

### Eliminado
- Archivos de plantilla (`WeatherForecast`, `Class1`, `UnitTest1`).
