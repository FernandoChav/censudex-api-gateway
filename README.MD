📚 Proyecto Censudex - Guía de Arquitectura y Despliegue (Taller 2)
===================================================================

**Autores:** 
 Fernando Chávez,
 Luis Felipe Ardiles Castillo,
 Daniel Alexis Tomigo Contreras,
 Sergio Leonardo Parada Espillico.

Este documento detalla la arquitectura de microservicios desarrollada en el Taller N°2. El sistema está diseñado sobre un **API Gateway** que orquesta la comunicación entre cinco microservicios independientes, garantizando la escalabilidad y modularidad requeridas por el contexto de Censudex.

I. ACERCA DE LA ARQUITECTURA Y DEPENDENCIAS
-------------------------------------------

El proyecto implementa una arquitectura de acoplamiento estricto a través del Gateway. Esto significa que **la indisponibilidad de cualquier servicio puede impactar el funcionamiento del Gateway**.

**Dependencias Críticas (Obligatorias):**
| Servicio   | Enlace de GitHub                                          | Función                                   | Dependencia de Conexión                                 |
|------------|------------------------------------------------------------|--------------------------------------------|----------------------------------------------------------|
| API Gateway | (Raíz del proyecto)                                      | Punto de Acceso Unificado.                | Requiere TODOS los servicios levantados.                 |
| Clients     | https://github.com/FernandoChav/censudex-clients-service | CRUD de Clientes/Usuarios.                | Auth Service lo consulta para login.                     |
| Auth        | https://github.com/FernandoChav/censudex-auth-service    | Generación y Validación de JWT (Token).   | Requiere Clients Service y Redis (Cache).                |
| Órdenes     | https://github.com/Luchardiles/censudex-orders-service   | Gestión de pedidos (gRPC).                 | Requiere Clients Service (para obtener datos del cliente). |
| Inventario  | https://github.com/Eltosergi/censudex-inventory-service  | Stock y Cantidades.                       | Consultado por el API Gateway (Inventario/Productos).    |
| Productos   | https://github.com/danukaz/censudex-products-service     | Catálogo de Productos (REST).             | Consultado por el API Gateway (Inventario/Productos).    |

II. PUNTOS DE ACCESO (ENDPOINTS EXPUESTOS)
--------------------------------------------------
## A. Clients Service (CRUD de Clientes)
| Método | Ruta                 | Descripción                                           | Seguridad   |
|--------|-----------------------|-------------------------------------------------------|-------------|
| POST   | /api/clients/create  | Crea un nuevo cliente en el sistema.                  | Ninguna     |
| GET    | /api/clients/{id}    | Obtiene los detalles completos de un cliente por ID. | JWT (Auth)  |
| PATCH  | /api/clients/{id}    | Actualiza parcialmente los datos del cliente.        | JWT (Auth)  |
| DELETE | /api/clients/{id}    | Desactiva (Soft Delete) a un cliente.                | JWT (Auth)  |

## B. Auth Service (Autenticación)
| Método | Ruta                        | Descripción                                             | Seguridad  |
|--------|-----------------------------|---------------------------------------------------------|------------|
| POST   | /api/auth/login             | Genera un JWT si las credenciales son válidas.         | Ninguna    |
| POST   | /api/auth/logout            | Cierra la sesión/invalida el token enviado.            | JWT (Auth) |
| GET    | /api/auth/validate-token    | Valida el token JWT enviado en la cabecera Authorization. | JWT (Auth) |

## C. Inventory + Products Services (Agregación)
| Método | Ruta                  | Descripción                                               | Seguridad  |
|--------|------------------------|-----------------------------------------------------------|------------|
| GET    | /api/inventory        | Obtiene la lista completa de productos con su stock.     | JWT (Auth) |
| GET    | /api/inventory/{id}   | Obtiene un producto específico con su stock.             | JWT (Auth) |
| PATCH  | /api/inventory/{id}   | Actualiza el stock de un producto específico.            | JWT (Auth) |
## D. Orders Service (gRPC)
| Método | Ruta                         | Descripción                                      | Seguridad  |
|--------|-------------------------------|--------------------------------------------------|------------|
| POST   | /api/orders                  | Crea un nuevo pedido.                           | Ninguna    |
| GET    | /api/orders                  | Obtiene todos los pedidos con filtros opcionales.| Ninguna    |
| GET    | /api/orders/{id}             | Obtiene un pedido específico por ID.            | Ninguna    |
| PUT    | /api/orders/{id}/status      | Actualiza el estado de un pedido.               | JWT (Auth) |
| PATCH  | /api/orders/{id}             | Cancela un pedido existente.                    | JWT (Auth) |


III. DESPLIEGUE ENTORNO DE PRODUCCIÓN (MODO DOCKER)
--------------------------------------------------

**Este es el modo canónico (recomendado por el taller) ya que simula el entorno productivo con Nginx (Balanceador) y DNS interno.**

### A. Requisitos de Infraestructura

1.  **Git:** Instalado.
    
2.  **Docker Desktop:** Instalado y en ejecución.
    

### B. Proceso de Inicialización (Automatizado)

1.  **Clonación de Repositorios:** Asegúrese de haber clonado **TODOS** los repositorios listados en la Sección I en la misma carpeta raíz._(Ejemplo: git clone \[URL\] para los 5 servicios.)_
    
2.  **Ubicación:** Abra su terminal en la **Carpeta Raíz** del proyecto (donde se encuentra docker-compose.yml).
    
3.  docker-compose up -d --build
    
4.  docker-compose ps_(Todos deben estar en estado Up o healthy.)_
    

### C. Acceso REST Principal

El punto de entrada unificado y balanceado es Nginx (Puerto 80):

*   **URL Base:** http://localhost
    
*   **Ejemplo de Consulta (Clientes):** GET http://localhost/api/clients/{id}
    

IV. DESARROLLO Y DEBUGGING LOCAL (MODO .NET SDK)
-------------------------------------------------

**Este modo es ÚNICAMENTE para realizar pruebas unitarias o depuración (debugging) en Visual Studio. No utiliza Nginx ni la red interna de Docker.**

### A. Requisitos CRÍTICOS

1.  **SDK de .NET 9.0:** Instalado.
    
2.  **Infraestructura Activa:** Las bases de datos deben estar disponibles en los puertos locales.
    

### B. Proceso de Inicialización Detallado

**Advertencia:** Este modo requiere que cada servicio se inicie en su puerto local.

1.  docker-compose up -d postgres-db redis-db
    
2.  Restaurar y Ejecutar Microservicios (5 Procesos en Background):
Antes de correr, es esencial ejecutar dotnet restore en cada carpeta de servicio.


```bash
# Clients Service
cd censudex-clients-service/ClientsService
dotnet restore

# Auth Service
cd ../../censudex-auth-service/AuthService
dotnet restore

# Orders Service
cd ../../censudex-orders-service
dotnet restore

# Inventory Service
cd ../censudex-inventory-service
dotnet restore

# Products Service
cd ../censudex-products-service
dotnet restore

# API Gateway
cd ../CENSUDEX-API-GATEWAY/ApiGatewayService
dotnet restore

```
3. Levantar los microservicios

Luego de restaurar, ejecuta cada servicio en su puerto correspondiente. Puedes hacerlo en ventanas de terminal separadas o usando el Debug de Visual Studio/VS Code.

Ejemplo usando terminal (bash / PowerShell):
```bash
# Clients
cd censudex-clients-service/ClientsService
dotnet run    # expone en http://localhost:5186 (según configuración)

# Auth
cd ../../censudex-auth-service/AuthService
dotnet run    # expone en http://localhost:5132

# Orders
cd ../../censudex-orders-service
dotnet run    # expone en http://localhost:5001

# Inventory
cd ../censudex-inventory-service
dotnet run    # expone en http://localhost:5233

# Products
cd ../censudex-products-service
dotnet run    # expone en http://localhost:3001


```

4. Levantar el API Gateway

Finalmente, levanta el Gateway después de tener los 5 servicios arriba.

Desde la carpeta del Gateway:
```bash
cd CENSUDEX-API-GATEWAY/ApiGatewayService

# (opcional pero recomendado si no lo hiciste antes)
dotnet restore
dotnet build

# Ejecutar el Gateway
dotnet run

```

| Servicio   | Ruta de Ejecución                                | Comando         | Puerto Usado |
|------------|---------------------------------------------------|-----------------|--------------|
| Clients    | censudex-clients-service/ClientsService          | dotnet run &    | 5186         |
| Auth       | censudex-auth-service/AuthService                | dotnet run &    | 5132         |
| Órdenes    | censudex-orders-service                          | dotnet run &    | 5001         |
| Inventario | censudex-inventory-service                       | dotnet run &    | 5233         |
| Productos  | censudex-products-service                        | dotnet run &    | 3001         |

    
3.  Iniciar el API Gateway (Último y al frente):
Ejecute el Gateway. Él buscará a los otros servicios en localhost:[PUERTO] (configurado en appsettings.json).
    

### C. Acceso Local Directo

*   **URL Base:** http://localhost:5004 (El puerto puede variar si no está en la configuración por defecto).
    

🆘 Troubleshooting y Verificación
| Problema                               | Diagnóstico y Verificación                                   | Solución Formal                                                       |
|-----------------------------------------|---------------------------------------------------------------|------------------------------------------------------------------------|
| Postman: 500 Interno (Connection Refused) | Un microservicio (ej. Auth) no está activo.                   | Verifique que los 5 servicios estén activos con `dotnet run`.         |
| Postman: 502 Bad Gateway                | El contenedor `apigateway-service` no está corriendo.         | Revisar logs: `docker logs censudex-apigateway-net`.                  |
| Error: Compilación fallida en Docker    | Faltan dependencias o `dotnet restore` no se ejecutó.         | Restaurar dependencias y verificar el Dockerfile con `dotnet restore`. |
| Problemas de conectividad BD            | Contraseña o host incorrecto en `docker-compose.yml`.         | Revisar cadena de conexión (ej. Host=postgres-db).                    |
