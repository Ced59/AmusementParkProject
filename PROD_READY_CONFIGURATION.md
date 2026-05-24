# Configuration prod-ready

Le projet conserve volontairement le comportement actuel en développement :

- emails mockés dans la console ;
- front attendu sur `http://localhost:4200` ;
- CORS autorisé pour `http://localhost:4200`.

## Passage en production sans modifier le code

Le back est désormais prêt à être piloté uniquement par configuration.

### Variables d'environnement minimales à injecter

```bash
ASPNETCORE_ENVIRONMENT=Production
AllowedHosts=amusement-parks.fun;www.amusement-parks.fun;localhost;127.0.0.1;amusementpark-api
Authentication__Local__FrontendBaseUrl=https://amusement-parks.fun
Cors__AllowedOrigins__0=https://amusement-parks.fun
ForwardedHeaders__ForwardLimit=2
ForwardedHeaders__KnownProxies__0=127.0.0.1
ForwardedHeaders__KnownProxies__1=::1
ForwardedHeaders__KnownNetworks__0=172.30.31.0/24
ForwardedHeaders__AllowedHosts__0=amusement-parks.fun;www.amusement-parks.fun;localhost;127.0.0.1
Security__ContentSecurityPolicy__Enabled=true
Security__ContentSecurityPolicy__ReportOnly=true
Security__ContentSecurityPolicy__ReportUri=/security/csp-report
RateLimiting__Authentication__Login__PermitLimit=5
RateLimiting__Authentication__Login__WindowSeconds=60
RateLimiting__Authentication__ExternalLogin__PermitLimit=10
RateLimiting__Authentication__ExternalLogin__WindowSeconds=60
RateLimiting__Authentication__RefreshToken__PermitLimit=30
RateLimiting__Authentication__RefreshToken__WindowSeconds=60
RateLimiting__Authentication__Registration__PermitLimit=5
RateLimiting__Authentication__Registration__WindowSeconds=900
RateLimiting__Authentication__EmailChallenge__PermitLimit=3
RateLimiting__Authentication__EmailChallenge__WindowSeconds=900
RateLimiting__Authentication__PasswordReset__PermitLimit=5
RateLimiting__Authentication__PasswordReset__WindowSeconds=900
Email__Mode=Smtp
Email__Host=smtp.hostinger.com
Email__Port=587
Email__UseSsl=false
Email__UseStartTls=true
Email__Username=noreply@amusement-parks.fun
Email__Password=VOTRE_SECRET
Email__FromAddress=noreply@amusement-parks.fun
Email__FromName=Amusement Park
```

## Verrouillage Host M18.2

`appsettings.json` ne contient plus de wildcard global. Le wildcard `AllowedHosts=*` est réservé au profil `Development`. En production, `AllowedHosts` doit être injecté explicitement par variable d'environnement ou par le fichier `.env` de déploiement.

La valeur recommandée pour le déploiement Docker actuel est :

```bash
AllowedHosts=amusement-parks.fun;www.amusement-parks.fun;localhost;127.0.0.1;amusementpark-api
```

Les hôtes locaux et le nom de service Docker sont conservés pour ne pas casser les healthchecks internes ni les futurs appels serveur-à-serveur.


## Forwarded Headers M18.3

L'API ne vide plus les réseaux/proxys connus sans les reconfigurer. Les headers `X-Forwarded-For`, `X-Forwarded-Proto` et `X-Forwarded-Host` sont maintenant acceptés uniquement depuis les proxys/réseaux configurés.

Pour le déploiement Docker actuel, `backend_private` est fixé à `172.30.31.0/24` et cette plage est injectée dans `FORWARDED_HEADERS_KNOWN_NETWORKS`. Si la plage Docker doit changer pour éviter un conflit VPS, modifier à la fois `BACKEND_PRIVATE_SUBNET` et `FORWARDED_HEADERS_KNOWN_NETWORKS`.


## CSP Report-Only M18.4

Le front Nginx ajoute désormais une `Content-Security-Policy-Report-Only` sur les réponses publiques. Le mode Report-Only permet de détecter les violations sans bloquer Angular, Google Identity, Matomo, Leaflet/OpenStreetMap ou les images API.

L'API expose aussi une configuration CSP et un endpoint technique anonyme :

```http
POST /security/csp-report
```

Derrière le front, le navigateur envoie les rapports à :

```http
POST /api/security/csp-report
```

Pour le passage M18.5, ne basculer `Security__ContentSecurityPolicy__ReportOnly=false` qu'après validation des rapports collectés.

M18.5 est volontairement différé tant que l'application n'a pas été testée sur le vrai environnement de production/staging. Il faudra le reprendre après déploiement réel pour passer de Report-Only à enforce.

## Rate limiting auth M18.6

Les endpoints auth publics sensibles ont des policies dédiées en plus du quota global IP : login, OAuth externe, refresh-token, inscription, confirmation/renvoi email, forgot-password et reset-password.

Les seuils prod par défaut sont configurables par environnement. Ils sont volontairement plus stricts que la navigation publique, car ces routes sont les principales surfaces de brute force, d'abus d'inscription et de spam email.

## Règle de sélection du sender mail

- `Email:Mode=Console` => `ConsoleEmailSender`
- `Email:Mode=Smtp` => `SmtpEmailSender`

## Fichier d'exemple

Le fichier `deploy/.env.production.example` est fourni comme base de configuration pour le déploiement Docker/VPS.

## M18.9 — Scans de dépendances CI

La CI ajoute un job `dependency-security` qui exécute :

```bash
dotnet list AmusementPark.sln package --vulnerable --include-transitive
npm audit --audit-level=moderate
npm audit signatures
```

Les rapports sont archivés dans `dependency-security-reports`. Le premier passage est non bloquant pour les vulnérabilités détectées, mais visible par warnings GitHub Actions.

## M18.10 — CORS et secrets prod

CORS est maintenant strictement configuré :

- origins explicites uniquement ;
- wildcard interdit avec credentials ;
- localhost interdit hors `Development` ;
- origins racine uniquement, sans chemin ni query string ;
- méthodes et headers autorisés listés explicitement.

Variables recommandées :

```bash
PUBLIC_BASE_URL=https://amusement-parks.fun
PUBLIC_WWW_BASE_URL=https://www.amusement-parks.fun
```

Le déploiement valide le `.env` avec :

```bash
deploy/scripts/validate-production-env.sh
```

La validation refuse les placeholders, les secrets manquants, `ALLOWED_HOSTS=*`, `FORWARDED_HEADERS_ALLOWED_HOSTS=*`, une `JWT_KEY` de moins de 32 caractères, les URL publiques non HTTPS, et les secrets Google OAuth absents.

Le fichier `deploy/.env.production.example` est restauré et sert de base documentaire, mais ne doit jamais être utilisé tel quel en production.


## Note `.env` robuste

Les scripts de déploiement ne font plus de `source .env` direct. Ils passent par `deploy/scripts/env-loader.sh`, afin que des valeurs contenant des `;`, des espaces ou certains caractères de secrets ne soient pas interprétées comme du code Bash.

## M19 — SEO minimal à vérifier avant MVP

Après déploiement sur le vrai domaine, vérifier :

```bash
curl -I https://amusement-parks.fun/fr/home
curl https://amusement-parks.fun/robots.txt
curl https://amusement-parks.fun/sitemap.xml
```

Attendus :

- les pages publiques ont `title`, meta description, canonical HTTPS, robots `index,follow` et alternates `hreflang` HTTPS ;
- admin, compte et auth restent en `noindex,nofollow` ;
- la 404 publique est en `noindex,follow` ;
- `robots.txt` référence le sitemap racine ;
- `sitemap.xml` ne contient que des URLs publiques.

## M20 — SSR public réellement servi

Le front de production doit maintenant être traité comme un service Node SSR, pas comme un simple dossier statique Nginx.

Points de configuration critiques :

- Nginx Proxy Manager pointe vers `127.0.0.1:${PUBLIC_HTTP_PORT:-18080}` ;
- le container front écoute en interne sur `4000` ;
- l'API reste privée et accessible depuis le SSR via `FRONT_SSR_API_INTERNAL_URL=http://api:8080` ;
- `/api`, `/robots.txt` et `/sitemap.xml` sont proxifiés par le serveur SSR ;
- les pages publiques SEO sont SSR ;
- admin/profil/auth restent CSR + noindex ;
- `ng serve` reste un mode dev rapide, mais ne valide pas le comportement SSR prod.

Avant exposition publique sérieuse, tester au minimum :

```bash
curl -i https://amusement-parks.fun/en/parks
curl -i https://amusement-parks.fun/sitemap.xml
curl -i https://amusement-parks.fun/api/health
```

Puis lancer le smoke test SSR depuis le front :

```bash
PUBLIC_BASE_URL=https://amusement-parks.fun npm run seo:ssr-smoke
```

## HTTPS public

En production, Nginx Proxy Manager doit exposer le domaine public en HTTPS avec **Force SSL** activé. Le conteneur front écoute en HTTP interne sur `127.0.0.1:${PUBLIC_HTTP_PORT:-18080}`, mais le trafic public doit être redirigé de `http://` vers `https://`.

Le serveur Angular SSR dispose aussi de `SSR_FORCE_HTTPS=true` en production. Cette protection redirige en 308 quand le reverse proxy transmet `X-Forwarded-Proto: http`. Elle ne remplace pas la configuration NPM, mais limite le risque d'oubli.


## Première production — alignement avec l’environnement local prod-like

La version validée localement avec Angular SSR, NPM, Mongo, MinIO et Matomo a entraîné les ajustements suivants pour la prod :

```bash
PUBLIC_HTTP_PORT=18080
MINIO_API_PORT=19000
MINIO_CONSOLE_PORT=19001
SSR_ALLOWED_HOSTS=amusement-parks.fun;www.amusement-parks.fun;localhost;127.0.0.1
SSR_FORCE_HTTPS=true
SSR_CSP_ALLOW_LOCAL_DEV_SOURCES=false
```

Le VPS conserve son Nginx Proxy Manager existant. Le compose production AmusementPark n’installe pas NPM : il publie uniquement le front SSR sur `127.0.0.1:18080`, puis NPM doit router `amusement-parks.fun` vers ce port avec SSL + Force SSL.

Le workflow de production est volontairement protégé par `PRODUCTION_DEPLOY_ENABLED=true` afin d’éviter un déploiement accidentel avant configuration DNS/NPM/secrets.

La liste exhaustive des secrets et variables GitHub attendus se trouve dans :

```text
docs/deploy/github-production-secrets-and-vars.md
```

Le plan de première mise en production et les commandes de diagnostic VPS sont dans :

```text
docs/deploy/production-cicd-first-release.md
```
