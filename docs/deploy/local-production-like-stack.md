# Stack local proche production

## Objectif

Ce stack local sert à tester les aspects difficiles à valider avec `ng serve` seul :

- Angular SSR réellement servi par Node ;
- reverse proxy via Nginx Proxy Manager ;
- API privée derrière le front ;
- MongoDB ;
- MinIO ;
- Matomo local ;
- cookies HTTP-only ;
- CSP Report-Only ;
- `robots.txt` et `sitemap.xml` ;
- premiers smoke tests SEO/SSR ;
- mesures Core Web Vitals dont le CLS.

Ce n'est pas une prod parfaite : le HTTPS public, le vrai domaine, les certificats et Google OAuth devront encore être validés en staging/prod. En local, rester en HTTP par défaut sauf si un certificat local est explicitement configuré dans NPM.

## Démarrage Windows

Depuis la racine du repository :

```powershell
copy deploy\local\.env.local.example deploy\local\.env.local
.\deploy\local\start-local-prod.ps1 -Build
```

Accès direct SSR, sans NPM :

```txt
http://localhost:14000/en/home
```

Accès Nginx Proxy Manager :

```txt
http://localhost:18181
```


## Regroupement dans Docker Desktop

Le stack est lancé avec le projet Docker Compose :

```txt
amusementpark-local-prod
```

Dans Docker Desktop, les conteneurs doivent donc être regroupés sous ce projet/app. Les conteneurs gardent aussi des noms explicites du type `amusementpark-local-api`, `amusementpark-local-front-ssr`, `amusementpark-local-mongodb`, etc.

Tu peux vérifier côté terminal avec :

```powershell
docker compose --project-name amusementpark-local-prod --env-file deploy\local\.env.local -f deploy\local\compose.local-prod.yml ps
```


## Ports locaux par défaut

Pour éviter les collisions avec l’environnement de dev classique, la préproduction locale utilise volontairement des ports hôte non standards :

| Service | Port hôte local | Port interne Docker |
|---|---:|---:|
| Nginx Proxy Manager HTTP | `18080` | `80` |
| Nginx Proxy Manager admin | `18181` | `81` |
| Nginx Proxy Manager HTTPS local | `18443` | `443` |
| Angular SSR direct | `14000` | `4000` |
| MongoDB inspection locale | `27018` | `27017` |
| MinIO API locale | `19000` | `9000` |
| MinIO console locale | `19001` | `9001` |

Les conteneurs communiquent entre eux avec les ports internes standards. Les ports décalés ne concernent que l’accès depuis Windows.

Si un `.env.local` existant contient encore les anciens ports par défaut (`8080`, `4000`, `27017`, `9000`, etc.), `start-local-prod.ps1` les migre automatiquement vers les nouveaux ports tant qu’ils n’ont pas été personnalisés.

Avant de lancer Docker Compose, le script vérifie aussi que les ports choisis ne sont pas déjà pris par un autre processus ou un autre stack Docker. En cas de conflit, modifier `deploy/local/.env.local`.

## Configuration NPM locale

Créer un Proxy Host dans Nginx Proxy Manager :

```txt
Domain Name: amusement.localhost
Scheme: http
Forward Hostname / IP: front
Forward Port: 4000
Websockets: enabled
```

Ensuite ouvrir :

```txt
http://amusement.localhost:18080/en/home
```

Si `amusement.localhost` ne résout pas sur ta machine, ajouter temporairement dans le fichier hosts Windows :

```txt
127.0.0.1 amusement.localhost matomo.amusement.localhost minio.amusement.localhost
```

## Matomo local

Le build `local-production` utilise volontairement `matomoSiteId: 1`. La production officielle conserve `matomoSiteId: 4` sur `https://matomo.cedric-caudron.com/`.

Pour éviter les erreurs CSRF Matomo liées à un reverse proxy local HTTP sur port non standard, l'instance Matomo locale est exposée directement sur un port hôte dédié :

```txt
http://localhost:18082
```

La configuration Angular `local-production` pointe donc vers :

```txt
matomoSiteId: 1
matomoTrackerUrl: http://localhost:18082/
```

Matomo reste dans le même projet Docker et utilise la base MariaDB interne `matomo-db`. Le Proxy Host NPM `matomo.amusement.localhost` devient optionnel et n'est plus recommandé pour l'administration locale de Matomo.

## MinIO local

Console directe :

```txt
http://localhost:19001
```

Identifiants par défaut dans `.env.local.example` :

```txt
minioadmin / minioadmin123
```

## MongoDB local

Port exposé localement :

```txt
localhost:27018
```

La base applicative et l'utilisateur applicatif sont créés via `deploy/mongo-init.js`.

## Smoke test SEO/SSR

Après configuration NPM :

```powershell
.\deploy\local\seo-smoke-local.ps1
```

Ou directement :

```powershell
cd FRONT\AmusementPark
$env:PUBLIC_BASE_URL='http://amusement.localhost:18080'
npm run seo:ssr-smoke
```

Le test vérifie notamment :

- HTTP 2xx/3xx ;
- `<title>` ;
- meta description ;
- canonical ;
- contenu initial rendu dans `app-root`.

## Tester le CLS

Le CLS se teste mieux sur ce stack que via `ng serve`, car on se rapproche du rendu réel : SSR, images, fonts, headers et reverse proxy.

Recommandé :

1. ouvrir `http://amusement.localhost:18080/en/home` ;
2. DevTools Chrome > Lighthouse ;
3. mode mobile ;
4. cocher Performance + SEO ;
5. lancer plusieurs mesures ;
6. comparer surtout le CLS sur home, parks, park detail et item detail.

Les vrais seuils CI/Lighthouse restent plutôt pour M23, après stabilisation SSR.

## Arrêt

```powershell
.\deploy\local\stop-local-prod.ps1
```

Pour supprimer aussi les volumes :

```powershell
docker compose --project-name amusementpark-local-prod --env-file deploy\local\.env.local -f deploy\local\compose.local-prod.yml down -v
```

### Matomo local: admin direct + trusted hosts

For local-prod, Matomo is intentionally exposed directly on `http://localhost:18082` instead of going through the local Nginx Proxy Manager. This avoids local reverse-proxy CSRF/origin issues in the Matomo admin UI.

The startup script refreshes Matomo `trusted_hosts[]` automatically after `docker compose up` so that the admin UI accepts:

- `localhost:18082`
- `127.0.0.1:18082`
- `matomo.amusement.localhost`
- `matomo.amusement.localhost:18080`

The AmusementPark local-prod Angular bundle tracks against `http://localhost:18082/` and uses Matomo site id `1`.

The frontend loads `matomo.js` only after optional-cookie consent, then sends the page view explicitly through Matomo's HTTP tracking endpoint (`matomo.php`). This avoids relying on the automatic router integration during SSR/hydration and makes the expected local Network panel check simple: after accepting optional cookies, both `matomo.js` and `matomo.php` should appear.
