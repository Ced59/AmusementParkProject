# M18.10 — CORS et secrets production

## Objectif

M18.10 verrouille les derniers points de configuration avant exposition publique sérieuse :

- CORS limité aux origines publiques officielles ;
- aucun wildcard CORS avec cookies ;
- aucune origine localhost acceptée hors développement ;
- secrets production obligatoires avant déploiement ;
- validation automatisée du `.env` généré par la pipeline et du `.env` présent sur le VPS.

## CORS API

Le backend ne se contente plus d'un fallback silencieux vers `http://localhost:4200`.

Règles appliquées :

- en `Development`, si aucune origine n'est configurée, fallback vers `http://localhost:4200` ;
- hors `Development`, au moins une origine explicite est obligatoire ;
- `*` est interdit si `AllowCredentials=true` ;
- `localhost`, `127.0.0.1` et `::1` sont interdits hors `Development` ;
- les origines doivent être des origins HTTP/HTTPS racine, sans path, query string ni fragment.

Origines prod injectées par Docker Compose :

```bash
Cors__AllowedOriginsCsv=${PUBLIC_BASE_URL};${PUBLIC_WWW_BASE_URL}
```

Méthodes autorisées :

```txt
GET, POST, PUT, PATCH, DELETE, OPTIONS
```

Headers autorisés :

```txt
Authorization, Content-Type, Accept-Language, X-Requested-With
```

Headers exposés au navigateur :

```txt
Retry-After, X-Rate-Limit-Limit, X-Rate-Limit-Remaining, X-Rate-Limit-Reset
```

## Secrets production

Un script de validation a été ajouté :

```txt
deploy/scripts/validate-production-env.sh
```

Il est appelé :

- dans GitHub Actions, juste après génération du `.env` ;
- sur le VPS via `deploy/scripts/deploy.sh`, avant `docker compose pull/up`.

## Variables bloquantes

Le script refuse le déploiement si une valeur obligatoire manque ou utilise un placeholder.

Variables contrôlées :

```txt
API_IMAGE
FRONT_IMAGE
PUBLIC_BASE_URL
PUBLIC_DOMAIN
ALLOWED_HOSTS
FORWARDED_HEADERS_ALLOWED_HOSTS
FORWARDED_HEADERS_KNOWN_NETWORKS
MONGO_INITDB_ROOT_USERNAME
MONGO_INITDB_ROOT_PASSWORD
MONGO_APP_USERNAME
MONGO_APP_PASSWORD
MINIO_ROOT_USER
MINIO_ROOT_PASSWORD
JWT_KEY
JWT_ISSUER
JWT_AUDIENCE
GOOGLE_CLIENT_ID
GOOGLE_CLIENT_SECRET
GOOGLE_REDIRECT_URI
```

Contrôles spécifiques :

- `PUBLIC_BASE_URL` et `PUBLIC_WWW_BASE_URL` doivent être des origins `https://` sans path ;
- `ALLOWED_HOSTS` et `FORWARDED_HEADERS_ALLOWED_HOSTS` ne doivent pas contenir `*` ;
- `JWT_KEY` doit contenir au moins 32 caractères ;
- `EMAIL_MODE=Smtp` rend les paramètres SMTP obligatoires ;
- `EMAIL_MODE=Console` reste accepté pour un smoke test privé, mais génère un warning.

## Fichier exemple restauré

Le fichier suivant est de nouveau présent dans le dépôt :

```txt
deploy/.env.production.example
```

La pipeline l'inclut dans le bundle de déploiement. Sans ce fichier, la préparation du bundle échouerait.


## Note `.env` robuste

Les scripts de déploiement ne font plus de `source .env` direct. Ils passent par `deploy/scripts/env-loader.sh`, afin que des valeurs contenant des `;`, des espaces ou certains caractères de secrets ne soient pas interprétées comme du code Bash.
