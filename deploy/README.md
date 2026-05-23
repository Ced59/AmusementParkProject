# Déploiement production AmusementPark

Cette configuration est prévue pour un VPS qui possède déjà Nginx Proxy Manager.

## Exposition réseau prévue

- Nginx Proxy Manager expose publiquement `https://amusement-parks.fun`.
- Le container front est publié uniquement sur `127.0.0.1:${PUBLIC_HTTP_PORT:-8080}`.
- L'API filtre les en-têtes `Host` via `AllowedHosts`, injecté par la variable `ALLOWED_HOSTS`.
- L'API n'a aucun port public : elle est appelée par le front via `/api`.
- MongoDB n'a aucun port public.
- MinIO est lié à `127.0.0.1` seulement, pour accès par SSH tunnel ou par une règle NPM protégée si nécessaire.

## Configuration Nginx Proxy Manager

Créer un Proxy Host :

- Domain Name : `amusement-parks.fun` et éventuellement `www.amusement-parks.fun`.
- Scheme : `http`.
- Forward Hostname / IP : `127.0.0.1`.
- Forward Port : `8080` ou la valeur de `PUBLIC_HTTP_PORT`.
- Activer Websockets.
- Activer SSL + Force SSL + HTTP/2.

Ne crée pas de Proxy Host public pour l'API. L'API passe par `https://amusement-parks.fun/api`.

## Verrouillage AllowedHosts

La production ne doit pas utiliser `AllowedHosts=*`. Le déploiement injecte :

```bash
ALLOWED_HOSTS=amusement-parks.fun;www.amusement-parks.fun;localhost;127.0.0.1;amusementpark-api
```

- `amusement-parks.fun` et `www.amusement-parks.fun` couvrent les domaines publics.
- `localhost` et `127.0.0.1` couvrent les healthchecks internes.
- `amusementpark-api` anticipe les appels Docker internes, notamment pour le futur SSR.

Toute autre valeur de `Host` doit être rejetée en production.


## Durcissement Forwarded Headers

L'API accepte les en-têtes `X-Forwarded-*` uniquement depuis les proxys et réseaux explicitement configurés.

Variables recommandées pour le déploiement Docker actuel :

```bash
PUBLIC_EDGE_SUBNET=172.30.30.0/24
BACKEND_PRIVATE_SUBNET=172.30.31.0/24
FORWARDED_HEADERS_KNOWN_NETWORKS=172.30.31.0/24
FORWARDED_HEADERS_ALLOWED_HOSTS=amusement-parks.fun;www.amusement-parks.fun;localhost;127.0.0.1
FORWARDED_HEADERS_FORWARD_LIMIT=2
```

Si Nginx Proxy Manager tourne dans un autre réseau Docker et que son adresse apparaît dans `X-Forwarded-For`, ajouter ce réseau à `FORWARDED_HEADERS_KNOWN_NETWORKS`, séparé par `;`.


## CSP Report-Only M18.4

Le front Nginx sert une `Content-Security-Policy-Report-Only` sur les pages et assets publics. Elle ne bloque rien pour le moment : elle sert à détecter les chargements qui seraient refusés au futur passage en mode enforce.

Les rapports navigateur sont envoyés vers :

```bash
/api/security/csp-report
```

Puis proxifiés vers l'API interne :

```bash
/security/csp-report
```

Variables API disponibles :

```bash
CSP_ENABLED=true
CSP_REPORT_ONLY=true
CSP_REPORT_URI=/security/csp-report
```

Pour tester localement le vrai header front, utiliser le container Nginx du front plutôt que `ng serve`, puis vérifier :

```bash
curl -I http://127.0.0.1:8080/
```

La réponse doit contenir `Content-Security-Policy-Report-Only`.

Avant M18.5, conserver `CSP_REPORT_ONLY=true` et analyser les logs `SecurityReportsController`.

M18.5 reste à reprendre impérativement après le premier déploiement réel/staging : il faudra vérifier les rapports CSP sur le vrai domaine HTTPS, puis seulement basculer en mode enforce.

## Rate limiting auth M18.6

Le quota global IP reste actif, mais les endpoints d'authentification publics ont désormais des limites dédiées :

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

Ces limites ciblent login, OAuth externe, refresh-token, inscription, confirmation/renvoi email, forgot-password et reset-password. Elles s'appliquent par IP après traitement sécurisé des `ForwardedHeaders`.

## Secrets GitHub Actions nécessaires

### Accès VPS

- `VPS_HOST`
- `VPS_SSH_USER`
- `VPS_SSH_PRIVATE_KEY`
- `VPS_SSH_PORT` optionnel, défaut `22`
- `VPS_DEPLOY_PATH` optionnel, défaut `/opt/amusementpark`

### Secrets applicatifs prod

- `PROD_MONGO_ROOT_USERNAME`
- `PROD_MONGO_ROOT_PASSWORD`
- `PROD_MONGO_APP_USERNAME`
- `PROD_MONGO_APP_PASSWORD`
- `PROD_MONGO_DATABASE_NAME` optionnel, défaut `AmusementPark`
- `PROD_MINIO_ROOT_USER`
- `PROD_MINIO_ROOT_PASSWORD`
- `PROD_MINIO_BUCKET` optionnel, défaut `amusement-park-images`
- `PROD_JWT_KEY`
- `PROD_JWT_ISSUER`
- `PROD_JWT_AUDIENCE`
- `PROD_GOOGLE_CLIENT_ID`
- `PROD_GOOGLE_CLIENT_SECRET`
- `PROD_GOOGLE_REDIRECT_URI`
- `PROD_FACEBOOK_APP_ID`
- `PROD_FACEBOOK_APP_SECRET`

### Email prod

- `PROD_EMAIL_MODE` : `Console` ou `Smtp`
- `PROD_EMAIL_HOST`
- `PROD_EMAIL_PORT`
- `PROD_EMAIL_USE_SSL`
- `PROD_EMAIL_USE_STARTTLS`
- `PROD_EMAIL_USERNAME`
- `PROD_EMAIL_PASSWORD`
- `PROD_EMAIL_FROM_ADDRESS`
- `PROD_EMAIL_FROM_NAME`

### Variables GitHub optionnelles

- `PUBLIC_BASE_URL`, défaut `https://amusement-parks.fun`
- `PUBLIC_DOMAIN`, défaut `amusement-parks.fun`
- `ALLOWED_HOSTS`, défaut pipeline : `amusement-parks.fun;www.amusement-parks.fun;localhost;127.0.0.1;amusementpark-api`
- `PUBLIC_HTTP_PORT`, défaut `8080`
- `MINIO_API_PORT`, défaut `9000`
- `MINIO_CONSOLE_PORT`, défaut `9001`
- `MINIO_IMAGE`, pour changer l'image MinIO sans modifier le compose
- `CSP_ENABLED`, défaut `true`
- `CSP_REPORT_ONLY`, défaut `true` pendant M18.4
- `CSP_REPORT_URI`, défaut `/security/csp-report`
- `AUTH_RATE_LIMIT_LOGIN_LIMIT`, défaut `5`
- `AUTH_RATE_LIMIT_LOGIN_WINDOW_SECONDS`, défaut `60`
- `AUTH_RATE_LIMIT_EXTERNAL_LOGIN_LIMIT`, défaut `10`
- `AUTH_RATE_LIMIT_EXTERNAL_LOGIN_WINDOW_SECONDS`, défaut `60`
- `AUTH_RATE_LIMIT_REFRESH_TOKEN_LIMIT`, défaut `30`
- `AUTH_RATE_LIMIT_REFRESH_TOKEN_WINDOW_SECONDS`, défaut `60`
- `AUTH_RATE_LIMIT_REGISTRATION_LIMIT`, défaut `5`
- `AUTH_RATE_LIMIT_REGISTRATION_WINDOW_SECONDS`, défaut `900`
- `AUTH_RATE_LIMIT_EMAIL_CHALLENGE_LIMIT`, défaut `3`
- `AUTH_RATE_LIMIT_EMAIL_CHALLENGE_WINDOW_SECONDS`, défaut `900`
- `AUTH_RATE_LIMIT_PASSWORD_RESET_LIMIT`, défaut `5`
- `AUTH_RATE_LIMIT_PASSWORD_RESET_WINDOW_SECONDS`, défaut `900`

## Déclenchement

Le workflow `.github/workflows/production.yml` lance :

1. build backend ;
2. tests backend si un projet `*Tests.csproj` existe ;
3. tests frontend en Chrome Headless ;
4. build frontend production ;
5. build et push des images immuables sur GHCR ;
6. déploiement VPS uniquement sur `push` vers `master`.

Les pull requests vers `master` lancent la CI, mais ne déploient pas.

## Accès MinIO privé

Depuis ta machine :

```bash
ssh -L 9001:127.0.0.1:9001 <user>@<vps>
```

Puis ouvrir `http://127.0.0.1:9001`.

## Sauvegarde MongoDB

Sur le VPS, dans le dossier de déploiement :

```bash
./scripts/backup-mongo.sh
```


## Note MinIO

L'application crée le bucket applicatif au premier usage si celui-ci n'existe pas encore. Le service MinIO reste donc privé et ne nécessite pas de bootstrap public.

## M18.9 — Scans dépendances CI

Le workflow `.github/workflows/production.yml` contient maintenant un job `dependency-security` lancé avec les builds.

Il archive un artefact `dependency-security-reports` contenant :

- `dotnet-vulnerable.txt` pour les vulnérabilités .NET directes et transitives ;
- `npm-audit.json` et `npm-audit.txt` pour les vulnérabilités npm `moderate` et supérieures ;
- `npm-audit-signatures.txt` pour la vérification des signatures npm en best-effort.

Ce premier palier émet des warnings sans bloquer automatiquement le déploiement au premier rapport. Une fois les rapports stabilisés, le seuil pourra devenir bloquant pour `high`/`critical`.

## M18.10 — CORS et secrets production

La configuration CORS prod est volontairement restrictive :

```bash
PUBLIC_BASE_URL=https://amusement-parks.fun
PUBLIC_WWW_BASE_URL=https://www.amusement-parks.fun
```

Ces deux origins sont injectées dans l'API via `Cors__AllowedOrigins__0` et `Cors__AllowedOrigins__1`.

Règles backend :

- aucune origine wildcard si `Cors__AllowCredentials=true` ;
- aucune origine `localhost` hors environnement `Development` ;
- aucune origine avec path, query string ou fragment ;
- pas de fallback automatique vers `http://localhost:4200` en production.

Le script suivant valide le `.env` avant redémarrage :

```bash
./scripts/validate-production-env.sh .env
```

Il est exécuté deux fois :

1. dans GitHub Actions, juste après génération du `.env` ;
2. sur le VPS via `deploy.sh`, avant `docker compose pull/up`.

Il refuse notamment les placeholders, les secrets manquants, `AllowedHosts=*`, les URLs publiques non HTTPS, les clés JWT trop courtes, et les paramètres Google OAuth absents.


## Note `.env` robuste

Les scripts de déploiement ne font plus de `source .env` direct. Ils passent par `deploy/scripts/env-loader.sh`, afin que des valeurs contenant des `;`, des espaces ou certains caractères de secrets ne soient pas interprétées comme du code Bash.

## M19 — SEO technique public

Le front Nginx proxifie maintenant les documents SEO racine vers l'API :

- `GET /robots.txt` -> `amusementpark-api:8080/robots.txt`
- `GET /sitemap.xml` -> `amusementpark-api:8080/sitemap.xml`

La variable `PUBLIC_BASE_URL` alimente aussi `Seo__PublicBaseUrl`, utilisée pour produire les URLs absolues du sitemap et la directive `Sitemap:` de `robots.txt`. En production, cette valeur doit rester une origin racine en `https://` : elle sert aussi de référence SEO pour éviter des canonical/hreflang/sitemap en `http://`.

Variables optionnelles :

```env
SEO_DEFAULT_LANGUAGE=en
SEO_MAX_DYNAMIC_URLS_PER_TYPE=50
```
