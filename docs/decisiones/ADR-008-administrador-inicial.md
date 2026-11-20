# ADR-008: Administrador inicial

- **Estado:** Aceptada
- **Fecha:** 2026-11-20

## Contexto

El registro público solo crea Vendedores y los roles Admin y Almacén los asigna un Admin (ADR-005, ADR-007). Con una base nueva no existe ningún administrador, así que hace falta una forma segura de crear el primero.

## Decisión

- Al arrancar, un servicio (`AdminSeedHostedService`) ejecuta `AdminSeeder`, que crea un administrador a partir de la configuración `Seed:AdminEmail`, `Seed:AdminPassword` y `Seed:AdminFullName`. Los valores van en user-secrets o variables de entorno; `appsettings.Example.json` solo contiene marcadores.
- **Reglas del sembrador:**
  - Sin configuración, no hace nada y **no consulta la base de datos**; la API arranca aunque la base no esté disponible.
  - Si ya existe algún administrador, no hace nada. Es idempotente y **nunca resetea contraseñas**: cambiar `Seed:AdminPassword` después no afecta a un administrador ya creado.
  - Si el correo configurado ya pertenece a un usuario que no es administrador, **no lo asciende** y lo registra como advertencia. Si lo hiciera, quien registró ese correo antes del primer arranque (conociendo su propia contraseña) obtendría control total.
  - Correo y contraseña pasan por las mismas reglas que el registro público (`RegisterRequestValidator`).
  - Los dos campos deben configurarse juntos.
- **Errores:** una configuración inválida o incompleta **impide el arranque** (mejor fallar claro que quedar sin administrador). Un fallo de entorno (base caída, migraciones sin aplicar) solo se registra como error y la API sigue arrancando; `/health/ready` ya informa del estado de la base.
- Carreras entre dos instancias que arrancan a la vez: el índice único de correo garantiza un solo administrador; la instancia que pierde lo trata como "ya existe".
- La contraseña nunca se escribe en logs ni en mensajes de error.

## Alternativas descartadas

- Endpoint público de "primer administrador": cualquiera podría reclamarlo antes que el dueño del sistema.
- Administrador sembrado en una migración con contraseña fija: la credencial quedaría en el repositorio.
- Ascender automáticamente la cuenta existente: vulnerable al registro previo descrito arriba.
- Aplicar migraciones automáticamente al arrancar: es una decisión de despliegue aparte; se mantiene el paso explícito `dotnet ef database update`.

## Consecuencias

- Para crear el primer administrador basta definir los secretos y arrancar la API contra una base ya migrada.
- Cambiar la contraseña de un administrador existente se hará con un endpoint de usuario o con acceso a la base; el sembrador no sirve para eso a propósito.
- Una vez creado el administrador, los secretos `Seed:*` pueden eliminarse.
