# M18.6 — Rate limiting ciblé auth

## Objectif

Le rate limiting global IP existant reste utile pour protéger l'API contre le bruit général, mais il ne suffit pas pour les endpoints d'authentification.

M18.6 ajoute donc des limites explicites sur les routes publiques sensibles :

| Policy | Endpoint concerné | Défaut prod |
|---|---|---:|
| `auth-login` | `POST /auth/login` | 5 requêtes / 60 s / IP |
| `auth-external-login` | `POST /auth/external/{provider}` | 10 requêtes / 60 s / IP |
| `auth-refresh` | `POST /auth/refresh-token` | 30 requêtes / 60 s / IP |
| `auth-registration` | `POST /users` | 5 requêtes / 900 s / IP |
| `auth-email-challenge` | `POST /users/confirm-email`, `POST /users/resend-confirmation`, `POST /users/forgot-password` | 3 requêtes / 900 s / IP |
| `auth-password-reset` | `POST /users/reset-password` | 5 requêtes / 900 s / IP |

Les limites s'appliquent par IP après traitement des `ForwardedHeaders`. La fiabilité dépend donc du durcissement M18.3 : seuls les proxys/réseaux autorisés doivent pouvoir fournir `X-Forwarded-For`.

## Choix technique

Le projet conserve le middleware `AspNetCoreRateLimit` existant pour le quota global :

```json
"IpRateLimiting": {
  "GeneralRules": [
    {
      "Endpoint": "*",
      "Period": "1s",
      "Limit": 30
    }
  ]
}
```

M18.6 ajoute en complément les policies natives ASP.NET Core via `EnableRateLimiting`, uniquement sur les actions sensibles. Cela évite de limiter tout le site public et rend le code plus lisible : chaque action sensible porte sa policy explicitement.

## Fichiers concernés

- `API/AmusementPark.WebAPI/Configuration/AuthenticationRateLimitingSettings.cs`
- `API/AmusementPark.WebAPI/Configuration/FixedWindowRateLimitSettings.cs`
- `API/AmusementPark.WebAPI/RateLimiting/RateLimitPolicyNames.cs`
- `API/AmusementPark.WebAPI/DependencyInjection/RateLimitingServiceCollectionExtensions.cs`
- `API/AmusementPark.WebAPI/DependencyInjection/WebApplicationPipelineExtensions.cs`
- `API/AmusementPark.WebAPI/Controllers/AuthController.cs`
- `API/AmusementPark.WebAPI/Controllers/UsersController.cs`

## Réponse attendue en cas de dépassement

L'API renvoie `429 Too Many Requests` avec un corps JSON minimal :

```json
{
  "statusCode": 429,
  "message": "Too many authentication requests. Please retry later.",
  "traceId": "..."
}
```

Quand disponible, le header `Retry-After` est également renseigné.

## Configuration production

Les valeurs par défaut sont dans `appsettings.json`, mais peuvent être surchargées par variables d'environnement :

```bash
AUTH_RATE_LIMIT_LOGIN_LIMIT=5
AUTH_RATE_LIMIT_LOGIN_WINDOW_SECONDS=60
AUTH_RATE_LIMIT_EXTERNAL_LOGIN_LIMIT=10
AUTH_RATE_LIMIT_EXTERNAL_LOGIN_WINDOW_SECONDS=60
AUTH_RATE_LIMIT_REFRESH_TOKEN_LIMIT=30
AUTH_RATE_LIMIT_REFRESH_TOKEN_WINDOW_SECONDS=60
AUTH_RATE_LIMIT_REGISTRATION_LIMIT=5
AUTH_RATE_LIMIT_REGISTRATION_WINDOW_SECONDS=900
AUTH_RATE_LIMIT_EMAIL_CHALLENGE_LIMIT=3
AUTH_RATE_LIMIT_EMAIL_CHALLENGE_WINDOW_SECONDS=900
AUTH_RATE_LIMIT_PASSWORD_RESET_LIMIT=5
AUTH_RATE_LIMIT_PASSWORD_RESET_WINDOW_SECONDS=900
```

Ces variables sont propagées par `deploy/compose.prod.yml` vers les clés .NET :

```bash
RateLimiting__Authentication__Login__PermitLimit
RateLimiting__Authentication__Login__WindowSeconds
```

Et ainsi de suite pour chaque policy.

## Configuration développement

`appsettings.Development.json` contient des seuils un peu plus permissifs pour faciliter les tests manuels locaux, tout en gardant le comportement activé.

## Tests manuels rapides

Exemple PowerShell sur le login local :

```powershell
1..10 | ForEach-Object {
  Invoke-WebRequest `
    -Uri "https://localhost:44391/auth/login" `
    -Method POST `
    -ContentType "application/json" `
    -Body '{"email":"wrong@example.com","password":"bad"}' `
    -SkipCertificateCheck
}
```

Après dépassement du seuil configuré, la réponse doit passer en `429`.

## Points de vigilance

- Les limites actuelles sont volontairement prudentes pour une MVP.
- Si Google OAuth ou le refresh token semblent trop limités en usage réel, augmenter uniquement la policy concernée.
- Ne pas remplacer ce mécanisme par un simple rate limit global : login, register et forgot/reset password doivent rester explicitement protégés.
