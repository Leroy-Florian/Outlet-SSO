# Outlet SSO

Bounded context **Identity** (SSO / accès) du projet Outlet, extrait du monorepo
`Outlet-CLI` afin que la CLI reste publique tandis que l'identité vit dans un repo
dédié.

Gère les **utilisateurs** et les **Personal Access Tokens (PAT)** pour l'accès
machine. C'est un **hexagone autonome** (Domain → Application → Infrastructure)
posé sur le kernel partagé `Outlet.Kernel.Shared`, sans aucune dépendance vers les
autres contextes Outlet (Core / Cloud).

## Structure

```
src/
  Kernel.Shared/Outlet.Kernel.Shared/        building blocks DDD (Result, Mediator, ValueObject…)
  Outlet.Identity.Domain/                    agrégats User + PersonalAccessToken, VOs, events
  Outlet.Identity.Application/               ports + use cases (Register / Issue / Revoke)
  Outlet.Identity.Infrastructure/            EF Core (PostgreSQL), ASP.NET Identity, hashing, migrations
tests/
  Outlet.Identity.{Domain,Application,Infrastructure}.UnitTests/
  Outlet.ArchitectureTests/                  gate : DDD + hexagonal + conventions
```

## Build & test

```bash
dotnet build Outlet.Sso.slnx -c Release
dotnet test  Outlet.Sso.slnx
```

Requiert le SDK **.NET 10** (C# 14). Les tests sont hermétiques (SQLite in-memory,
zéro réseau).

## Règles

Les conventions strictes (DDD, architecture hexagonale, tests d'architecture
NetArchTest, collection expressions, primary constructors, CPM, fakes écrits main)
sont reprises **à l'identique** du repo d'origine et documentées dans
[`CLAUDE.md`](./CLAUDE.md). Elles sont verrouillées par `tests/Outlet.ArchitectureTests`.
