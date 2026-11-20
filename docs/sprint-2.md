# Sprint 2: Seguridad

## Objetivo

Autenticar usuarios con JWT, restringir los endpoints por rol (Admin, Vendedor, Almacén) y dejar una forma segura de crear y administrar usuarios.

## Bloques completados

| Bloque | Contenido |
|---|---|
| 2.1 | Entidades `User` y `Role`, roles sembrados por migración, `IPasswordHasher` (PBKDF2 de Identity) y ADR-005 |
| 2.2 | Registro, login y `GET /api/auth/me` con JWT; validación de `JwtOptions` al arrancar; límite de intentos por IP; ADR-006 |
| 2.3 | Políticas por capacidad, autenticación obligatoria por defecto y `/api/users` para el Admin; ADR-007 |
| 2.4 | Administrador inicial desde user-secrets al arrancar; ADR-008 |

## Resultado

- Compilación con 0 advertencias y 0 errores.
- 147 pruebas pasando: 39 unitarias y 108 de integración contra SQL Server real.
- Cobertura de seguridad: hash de contraseñas, tokens alterados, expirados o con firma, emisor o audiencia incorrectos, matriz completa rol × política, límite de intentos, autoprotección de administradores y escenarios de abuso del sembrador.

## Decisiones técnicas

- Modelo propio de usuarios y roles (un rol por usuario), sin el Identity completo (ADR-005).
- JWT HS256 de vida corta, sin refresh tokens; la clave se valida al arrancar (ADR-006).
- Login sin filtrar información: misma respuesta 401 para correo inexistente, contraseña errónea o usuario inactivo, con verificación de hash de relleno para igualar tiempos.
- Registro público siempre como Vendedor; el rol enviado por el cliente se ignora.
- Autorización por políticas con política por defecto "autenticado": olvidar un atributo nunca deja un endpoint abierto (ADR-007).
- Un administrador no puede cambiar su propio rol ni desactivarse, de modo que el sistema nunca queda sin administradores.
- El administrador inicial nunca asciende cuentas existentes ni resetea contraseñas (ADR-008).
- Separé el bloque 2.3 original en 2.3 y 2.4 para mantener un commit por tema.
- Se desactiva CA1873 junto con CA1848 por la misma razón (costo de `ILogger` con plantillas, irrelevante a esta escala).

## Pendientes y notas para los siguientes sprints

- Un token ya emitido conserva su rol y sigue siendo válido hasta expirar, aunque se cambie el rol o se desactive al usuario. Está documentado; refresh tokens y revocación quedan como mejora futura.
- El límite de intentos usa la IP de la conexión; detrás de un proxy inverso hay que configurar cabeceras reenviadas.
- Una ruta inexistente consultada sin token responde 401 en lugar de 404 (efecto de la política por defecto).
- Las políticas `ManageCustomers`, `ManageProducts`, `ManageInventory`, `ManageInvoices` y `ViewReports` ya existen y están probadas, pero se aplicarán a los endpoints en los sprints 3 a 6.
- La migración `AddUsersAndRoles` debe aplicarse a la base de desarrollo antes de arrancar con los secretos `Seed:*`.
- Descuento de facturas (por línea, global o ambos): se decide en el bloque 5.1.
