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

Ce n'est pas une prod parfaite : le HTTPS public, le vrai domaine, les certificats et Google OAuth devront encore être validés en staging/prod.

## Démarrage Windows

Depuis la racine du repository :

```powershell
copy deploy\local\.env.local.example deploy\local\.env.local
.\deploy\local\start-local-prod.ps1 -Build
```

Accès direct SSR, sans NPM :

```txt
http://localhost:4000/en/home
```

Accès Nginx Proxy Manager :

```txt
http://localhost:8181
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
http://amusement.localhost:8080/en/home
```

Si `amusement.localhost` ne résout pas sur ta machine, ajouter temporairement dans le fichier hosts Windows :

```txt
127.0.0.1 amusement.localhost matomo.amusement.localhost minio.amusement.localhost
```

## Matomo local

Matomo est lancé dans le stack pour tester le consentement et la future observabilité web.

Créer un Proxy Host NPM optionnel :

```txt
Domain Name: matomo.amusement.localhost
Scheme: http
Forward Hostname / IP: matomo
Forward Port: 80
```

Puis ouvrir :

```txt
http://matomo.amusement.localhost:8080
```

La configuration Angular `local-production` pointe vers cette URL Matomo locale.

## MinIO local

Console directe :

```txt
http://localhost:9001
```

Identifiants par défaut dans `.env.local.example` :

```txt
minioadmin / minioadmin123
```

## MongoDB local

Port exposé localement :

```txt
localhost:27017
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
$env:PUBLIC_BASE_URL='http://amusement.localhost:8080'
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

1. ouvrir `http://amusement.localhost:8080/en/home` ;
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
