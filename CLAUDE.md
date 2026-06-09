# Outlet SSO — Guide projet (CLAUDE.md)

> Extrait du monorepo `Outlet-CLI`. Ce repo isole le **contexte borné Identity** (SSO /
> accès) afin de garder la CLI publique tandis que l'identité vit séparément. Les
> conventions ci-dessous sont **identiques** à celles du repo d'origine et restent
> verrouillées par les tests d'architecture.

## Ce qu'est ce repo

Le **bounded context Identity** d'Outlet : gestion des **utilisateurs** et des
**Personal Access Tokens (PAT)** servant à l'accès machine. C'est un **hexagone
auto-contenu** (Domain → Application → Infrastructure) posé sur le kernel partagé
`Outlet.Kernel.Shared`.

Il ne référence aucun autre contexte Outlet (Core/Cloud) — la communication
inter-contextes se fait par **id et chaînes simples** (les scopes de token traversent
la frontière sous forme de `string`), jamais par import de types.

## Structure

```
src/Kernel.Shared/Outlet.Kernel.Shared/        ← building blocks DDD partagés (Result, Mediator, ValueObject…)
src/Kernel.Shared/Outlet.Kernel.Shared.UnitTests/
src/Outlet.Identity.Domain/                    ← agrégats User + PersonalAccessToken, VOs, events
src/Outlet.Identity.Application/               ← ports + use cases (Register / Issue / Revoke)
src/Outlet.Identity.Infrastructure/            ← EF Core (PostgreSQL), ASP.NET Identity, hashing, migrations
tests/Outlet.Identity.Domain.UnitTests/
tests/Outlet.Identity.Application.UnitTests/
tests/Outlet.Identity.Infrastructure.UnitTests/
tests/Outlet.ArchitectureTests/                ← LE gate : toute convention est testée
```

## Architecture technique (hexagonal + DDD)

### Layering (vérifié par `LayeredArchitectureTests`)

| Couche | Peut dépendre de |
|---|---|
| `Outlet.Kernel.Shared` | rien (jamais d'un contexte) |
| `Outlet.Identity.Domain` | Kernel uniquement |
| `Outlet.Identity.Application` | Domain + Kernel |
| `Outlet.Identity.Infrastructure` | Application + Domain + Kernel |

### Langage du domaine

- **Agrégats** : `User` (email, plan, membership ASP.NET Identity côté infra),
  `PersonalAccessToken` (hash, scopes, expiration, révocation).
- **Value Objects** : `UserId`, `EmailAddress`, `PersonalAccessTokenId`, `TokenHash`,
  `TokenScope` — `sealed`, ctor privé + factory `From(...)`/`Create(...)` qui valide.
- **Ports (`Application/Ports/`)** : `IUserRepository`, `IPersonalAccessTokenRepository`,
  `ITokenSecretFactory`. Les ports acceptent des VOs, pas des primitives (Tell, Don't Ask).
- **Use cases** : `{Action}{Entity}UseCase` implémentant `IUseCase<TCommand[, TResult]>`,
  retournent `Result`/`Result<T>`.
- **Events** : `{Entity}{Action}Event` (`UserRegisteredEvent`, `PersonalAccessTokenIssuedEvent`…).

### Règles par couche (toutes vérifiées par les tests d'architecture)

**Domain** :
- AUCUNE dépendance technique : pas de HTTP, JSON, DB, logging, Roslyn, MSBuild (`TechnicalDependencyTests`).
- Pas de `DateTime.Now`/`UtcNow` → injecter `ICurrentDateTimeProvider` (`DateTimeProviderConventionTests`).
- Synchrone uniquement, classes `sealed`, pas de setters publics, IDs fortement typés (`SealedAndSynchronousDomainTests`).
- Agrégats : ctor privé + factory statique retournant l'agrégat ou `Result<T>` (`DddAggregateTests`).
- Les agrégats se référencent par ID, jamais par objet.
- Exceptions domaine `sealed`, suffixe `Exception`.

**Application** :
- Mêmes interdits techniques que Domain (HTTP/JSON/DB/logging/Roslyn/MSBuild).
- Use cases : jamais d'exception pour une erreur métier → `Result`/`Result<T>` (`UseCaseConventionTests`).
- Commands/queries = records immuables.
- Pas de logique métier (déléguer au Domain).

**Infrastructure** :
- Implémente les ports, `sealed`, primary constructors.
- C'est ICI (et seulement ici) que vivent EF Core, Npgsql, ASP.NET Identity, le hashing.

### Isolation de contexte (`BoundedContextIsolationTests`)
- Identity ne dépend jamais de `Outlet.Core.*` ni `Outlet.Cloud.*` (vérifié par préfixe de namespace).

### Style C# (build = gate)

- **.NET 10, C# 14**, `ImplicitUsings`, `Nullable`, `EnforceCodeStyleInBuild` (Directory.Build.props).
- **Collection expressions obligatoires** : IDE0300→0306 en `error` (.editorconfig). `[.. xs.Where(...)]`, jamais `.ToList()` assigné.
- **Primary constructors obligatoires hors Domain** (`PrimaryConstructorConventionTests`). Opt-out rare : commentaire `// non-primary: <raison>`.
- **CPM** : toute version de package vit dans `Directory.Packages.props`, jamais dans un csproj.
- Interfaces préfixées `I`, naming vérifié par `NamingConventionTests`.

## Stratégie de tests

- **Zéro réseau**, hermétique. Persistance testée via **SQLite in-memory** (provider EF Core).
- **Fakes écrits main uniquement** — AUCUN framework de mock (pas de Moq/NSubstitute). Cf. `tests/Outlet.Identity.Application.UnitTests/Fakes/`.
- **Nommage** : `Should_<Effet>_When_<Condition>`. Pas de commentaires Given/When/Then — séparation visuelle par lignes vides.
- **Tests d'architecture** = non négociables : ils encodent ce document. Si un test d'archi gêne, on discute de la règle, on ne contourne pas le test.
- **Barre qualité** : ≥ 90 % de couverture Domain+Application. Un BC inachevé est une régression.

## Garde-fous

- **Zéro dette** : un finding est corrigé dans la session ou explicitement différé avec ticket.
- Ne pas réintroduire de dépendance vers Core/Cloud : ce repo doit rester un hexagone autonome.
- Toute migration EF Core est générée via `dotnet ef`, jamais éditée à la main de façon incohérente avec le snapshot.

## Commandes utiles

```bash
dotnet build Outlet.Sso.slnx -c Release        # 0 warning exigé (IDE03xx = erreurs)
dotnet test Outlet.Sso.slnx
```
