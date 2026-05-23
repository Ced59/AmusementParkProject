# Déploiement production AmusementPark

Cette configuration est prévue pour un VPS qui possède déjà Nginx Proxy Manager.

## Exposition réseau prévue

- Nginx Proxy Manager expose publiquement `https://amusement-parks.fun`.
- Le container front est publié uniquement sur `127.0.0.1:${PUBLIC_HTTP_PORT:-8080}`.
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
- `PUBLIC_HTTP_PORT`, défaut `8080`
- `MINIO_API_PORT`, défaut `9000`
- `MINIO_CONSOLE_PORT`, défaut `9001`
- `MINIO_IMAGE`, pour changer l'image MinIO sans modifier le compose

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
