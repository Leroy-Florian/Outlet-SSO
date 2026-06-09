# Registre interne Outlet-SSO

Items de registre **privés** servis à la CLI publique `outlet` comme une **source supplémentaire**.
On garde ici le code « copier-collable » lié à l'identité/SSO sans l'exposer dans le repo public de la CLI.

> Modèle Outlet : chaque préoccupation = un **port générique** (`outlet:contract`) + des
> **adapters interchangeables** (`outlet:adapter`). L'utilisateur **copie le code et le possède** —
> aucune dépendance runtime à Outlet. Cf. `registry-item.schema.json` pour le format de manifeste.

## Préoccupation `auth`

Authentification machine par **Personal Access Token** (bearer `Authorization: Bearer …`).
C'est la *colle de consommation* côté host : le bounded context Identity (agrégats, EF Core,
migrations) reste, lui, une **librairie/service** dont on dépend — il n'est volontairement pas
distribué en copier-coller.

| Item | Type | Contenu |
|---|---|---|
| `auth-pat-abstractions` | `outlet:contract` | `IPersonalAccessTokenAuthenticator`, `AuthenticatedToken`, `IPersonalAccessTokenStore`, `StoredAccessToken` — zéro dépendance externe. |
| `auth-pat-sha256` | `outlet:adapter` | `Sha256PersonalAccessTokenAuthenticator` + `TokenHashing` + Options + `AddSha256PersonalAccessTokenAuthentication()`. BCL uniquement (SHA-256, `TimeProvider`). |

### Brancher l'adapter

```csharp
// 1. ta persistance des tokens, projetée sur le port de lookup
services.AddScoped<IPersonalAccessTokenStore, MyTokenStore>();

// 2. une horloge (testable)
services.AddSingleton(TimeProvider.System);

// 3. l'adapter SHA-256
services.AddSha256PersonalAccessTokenAuthentication();
```

Swapper de stratégie de hash/validation = un autre adapter `auth-pat-*` derrière le même contrat.
