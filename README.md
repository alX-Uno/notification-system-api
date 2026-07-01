# Notification System API

API .NET 10 para gestión de órdenes y envío simulado de notificaciones.

## Requisitos

- .NET 10 SDK
- Docker + Docker Compose (para ejecutar SQL Server local)
- `dotnet-ef` para aplicar migraciones desde la CLI

## Archivos importantes

- docker-compose.yml — levanta SQL Server de desarrollo
- NotificationSystem.Api.csproj — contiene `UserSecretsId`
- appsettings.Development.json — archivo de ejemplo

## Resumen del flujo de configuración local

1. Levantar la base de datos (Docker)
2. Registrar secrets locales con `dotnet user-secrets`
3. Aplicar migraciones a la BD
4. Compilar y ejecutar la API
5. Probar endpoints (login → usar token → crear/leer órdenes)

## Paso a paso

1. Clonar el repo:

```bash
git clone https://github.com/alX-Uno/notification-system-api.git
cd notification-system-api
```

2. Levantar SQL Server (desde la raíz del repo):

```bash
docker compose up -d
```

El docker-compose.yml preconfigura un contenedor `sqlserver_dev` escuchando en el puerto `1433`. La contraseña usada en el compose es `TuPassword123!`.

3. Registrar user-secrets (desde la carpeta del proyecto)

```bash
cd NotificationSystem.Api
```

Si aún no se tiene `dotnet user-secrets`, añadir los valores locales:

```powershell
dotnet user-secrets set "Notification:SimulatedDelayMs" "500"
dotnet user-secrets set "Notification:FailureRatePercent" "10"

dotnet user-secrets set "Jwt:Key" "zuotdxZKfSHJpxtwk4v9SnzkmZqKeGLAQrEIxVcM31E"
dotnet user-secrets set "Jwt:Issuer" "NotificationSystem.Api"
dotnet user-secrets set "Jwt:Audience" "NotificationSystem.Api"
dotnet user-secrets set "Jwt:ExpireMinutes" "60"

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=NotificationSystemDb;User Id=sa;Password=TuPassword123!;TrustServerCertificate=True;"

dotnet user-secrets set "Auth:Username" "admin"
dotnet user-secrets set "Auth:Password" "Password123$"
```

Verificar:

```powershell
dotnet user-secrets list
```

4. Instalar `dotnet-ef` si no se tiene:

```bash
dotnet tool install --global dotnet-ef
```

5. Aplicar migraciones y crear la base de datos (desde NotificationSystem.Api)

```bash
dotnet ef database update
```

6. Compilar y ejecutar la API

```bash
dotnet build
dotnet run
```

## Endpoints principales (resumen)

- POST `/api/auth/login`  
  Body:

  ```json
  {
    "username": "admin",
    "password": "Password123$"
  }
  ```

  Respuesta: `{ "token": "<jwt>" }`

- POST `/api/orders` (protegido por JWT)  
  Body:

  ```json
  {
    "customerName": "Cliente",
    "totalAmount": 123.45
  }
  ```

  - Crea orden y dispara la notificación (intento guardado en DB).
  - Devuelve un DTO resumen de la orden.

- GET `/api/orders` (protegido)  
  Query params: `page`, `pageSize`
  - Devuelve lista de órdenes sin los `NotificationAttempts`.

- GET `/api/orders/{id}` (protegido)
  - Devuelve la orden con su lista de `NotificationAttempts`.
