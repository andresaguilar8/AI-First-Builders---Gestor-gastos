# AGENTS.md

## Propósito
App de gestión de gastos personales mensuales: registrar gastos (puntuales o recurrentes), marcarlos como pagados y ver un resumen por categoría.
Un único usuario autenticado, sin multi-tenant ni offline.

## Stack
- Backend: .NET 10, ASP.NET Core, Entity Framework Core
- Frontend: React 19, TypeScript 5
- Base de datos: PostgreSQL 16
- Gestor de paquetes frontend: pnpm

## Cómo correr
```bash
# Base de datos
docker compose up -d

# Backend
cd backend
dotnet restore
dotnet run

# Frontend
cd frontend
pnpm install
pnpm dev

# Tests
cd backend && dotnet test
cd frontend && pnpm test
```

## Qué NO hacer
- No guardar contraseñas en texto plano: siempre persistirlas con hash seguro (RNF-04).
- No modificar retroactivamente los registros de meses anteriores al editar un gasto recurrente (RF-06/RF-17).
- No pre-generar registros de meses futuros para gastos recurrentes: se proyectan al consultar cada período (RF-03).
