# M18.7 — ProblemDetails + traceId

## Objectif

Toutes les erreurs HTTP de l'API doivent sortir sous un contrat unique RFC 7807 : `application/problem+json`.

Le but est double :

- ne plus exposer de détails internes en production ;
- pouvoir rattacher une erreur utilisateur à un log serveur via `traceId`.

## Contrat final

Exemple générique :

```json
{
  "type": "https://amusement-parks.fun/problems/access-forbidden",
  "title": "Access is forbidden.",
  "status": 403,
  "detail": "You do not have permission to access this resource.",
  "instance": "/parks",
  "traceId": "0HN...",
  "errorCode": "authorization.forbidden"
}
```

Exemple validation :

```json
{
  "type": "https://amusement-parks.fun/problems/bad-request",
  "title": "Validation failed.",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/users",
  "errors": {
    "Email": ["The Email field is required."]
  },
  "traceId": "0HN...",
  "errorCode": "validation.model-state.invalid"
}
```

## Sources d'erreurs couvertes

- erreurs applicatives `ApplicationResult` ;
- validations automatiques `[ApiController]` / DataAnnotations ;
- erreurs manuelles de contrôleurs ;
- refus d'authentification `401` ;
- refus d'autorisation `403` ;
- comptes non activés / bloqués ;
- rate limiting `429` ;
- exceptions non gérées `500` ;
- statuts sans corps, notamment `404` technique.

## Décision importante

Il n'y a plus de compatibilité volontaire avec l'ancien format :

```json
{
  "statusCode": 400,
  "message": "..."
}
```

Le frontend ne parse plus cet ancien format. Si un endpoint renvoie autre chose que `ProblemDetails`, l'interface affiche le message générique. Cela force le projet à converger vers le contrat final avant MVP.

## Sécurité production

Les exceptions non gérées renvoient toujours :

```json
{
  "title": "Unexpected server error.",
  "status": 500,
  "detail": "An unexpected error occurred.",
  "traceId": "..."
}
```

Le détail technique reste uniquement dans les logs serveur avec le même `traceId`.

## Frontend

Le frontend consomme uniquement :

- `detail` en priorité ;
- `title` en fallback ;
- jamais `message` / `Message` / `statusCode`.

Les helpers concernés sont :

- `shared/models/contracts/api-error.model.ts`
- `shared/utils/security/error-display.helpers.ts`

## Tests manuels rapides

### Validation modèle

Envoyer un payload incomplet sur `POST /users` doit renvoyer `400 application/problem+json` avec `errors` et `traceId`.

### Auth absente

Appeler une route protégée sans token doit renvoyer `401 application/problem+json` avec `errorCode = authentication.required`.

### Autorisation insuffisante

Appeler une route admin avec un utilisateur non autorisé doit renvoyer `403 application/problem+json`.

### Rate limiting

Dépasser le seuil de `POST /auth/login` doit renvoyer `429 application/problem+json` et, si disponible, `Retry-After`.

### Exception non gérée

Une exception serveur doit renvoyer un message neutre et logguer l'exception avec le même `traceId`.
