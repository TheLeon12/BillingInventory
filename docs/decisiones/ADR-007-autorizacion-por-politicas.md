# ADR-007: Autorización por políticas

- **Estado:** Aceptada
- **Fecha:** 2026-11-20

## Contexto

Cada rol (Admin, Vendedor, Almacén) debe poder hacer solo lo que le corresponde. Los permisos cambian con el tiempo y no deben quedar dispersos como listas de roles en cada controlador.

## Decisión

- **Políticas por capacidad, no por rol.** Los controladores usan `[Authorize(Policy = Policies.X)]`; el mapeo política → roles vive en un solo archivo (`Api/Authorization/Policies.cs`).

| Política | Roles permitidos |
|---|---|
| `AdminOnly` | Admin |
| `ManageCustomers` | Admin, Vendedor |
| `ManageProducts` | Admin, Almacén |
| `ManageInventory` | Admin, Almacén |
| `ManageInvoices` | Admin, Vendedor |
| `ViewReports` | Admin, Vendedor |

- **Admin tiene acceso a todo.** Se incluye explícitamente en cada política.
- **Seguro por defecto:** se define una política por defecto (`FallbackPolicy`) que exige un usuario autenticado. Todo endpoint sin atributo queda protegido; lo público (`/health`, login, registro, raíz de Swagger) se marca con `[AllowAnonymous]`.
- **Lectura de catálogo:** consultar productos y clientes solo exige estar autenticado (cualquier rol), porque el Vendedor necesita ver productos al facturar y el Almacén necesita ver clientes en consultas. La escritura sí usa las políticas anteriores.
- **Códigos de respuesta:** sin token o token inválido → 401; token válido sin permiso → 403. Ambos con `ProblemDetails`.
- **Gestión de usuarios** (`/api/users`, política `AdminOnly`): listar con filtros, ver, cambiar rol y activar o desactivar. Un administrador no puede cambiar su propio rol ni desactivar su propia cuenta; como quien actúa siempre es un admin, esto garantiza que nunca se quede el sistema sin administradores.

## Consecuencias

- Una ruta inexistente consultada sin token responde 401 y no 404: la política por defecto no distingue rutas, lo que evita revelar qué rutas existen. Con token se obtiene el 404 habitual.
- El rol viaja en el JWT: un cambio de rol, o desactivar a un usuario, no afecta a tokens ya emitidos hasta que expiren (vigencia corta, ver ADR-006). Se deja documentado en Swagger.
- Cambiar quién puede hacer qué es editar una línea de `Policies.cs`; las pruebas de la matriz rol × política detectan cualquier cambio no intencionado.
