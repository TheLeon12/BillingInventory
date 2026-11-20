# ADR-005: Usuarios, roles y hash de contraseñas

- **Estado:** Aceptada
- **Fecha:** 2026-11-20

## Contexto

La API necesita autenticar usuarios y restringir endpoints por rol (Admin, Vendedor, Almacén). Hay que guardar contraseñas de forma segura sin depender de todo el sistema de ASP.NET Core Identity.

## Decisión

- **Modelo propio, ligero:** tablas `Users` y `Roles`, sin el Identity completo (sin claims por usuario, tokens de confirmación ni bloqueo integrado). Cada usuario tiene exactamente **un rol**, lo que cubre los requisitos y simplifica las políticas.
- **Roles fijos y sembrados por la migración** (`HasData`) con identificadores 1 (`Admin`), 2 (`Vendedor`) y 3 (`Almacen`). Los nombres no llevan acentos porque viajan en el claim del JWT y en las políticas. La fecha de creación es fija para que la migración sea determinista.
- **Hash de contraseñas:** `PasswordHasher<T>` de `Microsoft.Extensions.Identity.Core` (PBKDF2-HMAC-SHA512, sal aleatoria, iteraciones versionadas dentro del propio hash), detrás de la interfaz `IPasswordHasher` de Application. Si el hash usa parámetros antiguos, `Verify` devuelve `SuccessRehashNeeded` y el login podrá regenerarlo.
- **Correo único**, guardado en minúsculas y con índice único (SQL Server compara sin distinguir mayúsculas).
- **Integridad:** la clave foránea `Users.RoleId` usa `Restrict`; no se puede borrar un rol con usuarios.
- **Registro público:** siempre crea el rol Vendedor. Admin y Almacén los asigna un Admin (Sprint 2, bloque 2.3). El primer Admin se crea a partir de user-secrets (`Seed:*`).

## Alternativas descartadas

- ASP.NET Core Identity completo: aporta mucho que este proyecto no necesita y oculta la lógica que se quiere demostrar.
- Varios roles por usuario: más flexible, pero los requisitos no lo piden y complica las políticas y las pruebas.
- Hash propio con `Rfc2898DeriveBytes`: reimplementa algo que ya está resuelto y auditado.

## Consecuencias

- Las contraseñas nunca se guardan ni se registran en texto plano; el hash cabe en `PasswordHash` (500).
- Cambiar el rol de un usuario es un `UPDATE` de `RoleId`.
- Cambios futuros (bloqueo por intentos, refresh tokens) se agregan como tablas propias.
