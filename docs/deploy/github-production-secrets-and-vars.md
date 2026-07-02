# GitHub Actions — secrets et variables production

Cette liste correspond au workflow `.github/workflows/production.yml`.

## Secrets GitHub obligatoires

### Accès VPS

| Secret | Exemple / note |
|---|---|
| `VPS_HOST` | IP ou host SSH du VPS |
| `VPS_SSH_USER` | `root` ou utilisateur de déploiement |
| `VPS_SSH_PRIVATE_KEY` | clé privée SSH complète |
| `VPS_SSH_PORT` | `22` si port SSH standard |

### Registry GHCR

| Secret | Exemple / note |
|---|---|
| `GHCR_USERNAME` | login GitHub autorisé à lire les packages |
| `GHCR_TOKEN` | PAT avec `read:packages` si images GHCR privées |

Si les images GHCR sont publiques, ces deux valeurs peuvent rester vides, mais il est recommandé de les configurer pour éviter les surprises au premier déploiement.

### MongoDB production

| Secret | Exemple / note |
|---|---|
| `PROD_MONGO_ROOT_USERNAME` | utilisateur root Mongo, différent du local |
| `PROD_MONGO_ROOT_PASSWORD` | mot de passe long et aléatoire |
| `PROD_MONGO_APP_USERNAME` | utilisateur applicatif |
| `PROD_MONGO_APP_PASSWORD` | mot de passe long et aléatoire |

### MinIO production

| Secret | Exemple / note |
|---|---|
| `PROD_MINIO_ROOT_USER` | utilisateur root MinIO, différent de `minioadmin` |
| `PROD_MINIO_ROOT_PASSWORD` | mot de passe long et aléatoire |

### Auth / JWT

| Secret | Exemple / note |
|---|---|
| `PROD_JWT_KEY` | minimum 32 caractères, idéalement 64+ aléatoires |

### Google OAuth

| Secret | Exemple / note |
|---|---|
| `PROD_GOOGLE_CLIENT_ID` | client ID Google prod |
| `PROD_GOOGLE_CLIENT_SECRET` | secret Google prod |

Le redirect URI prod attendu est :

```text
https://amusement-parks.fun/api/auth/external/google/callback
```

### Facebook OAuth, si activé

| Secret | Exemple / note |
|---|---|
| `PROD_FACEBOOK_APP_ID` | peut rester vide si Facebook n’est pas activé |
| `PROD_FACEBOOK_APP_SECRET` | peut rester vide si Facebook n’est pas activé |

### SMTP, si `PROD_EMAIL_MODE=Smtp`

| Secret | Exemple / note |
|---|---|
| `PROD_EMAIL_HOST` | host SMTP |
| `PROD_EMAIL_USERNAME` | login SMTP |
| `PROD_EMAIL_PASSWORD` | mot de passe SMTP |

Adresses mail a creer pour la production :

- `noreply@amusement-parks.fun` : expediteur automatique pour confirmation de compte, reset password et notifications internes.
- `contact@amusement-parks.fun` : adresse publique de contact et d'echange avec les utilisateurs.
- `admin@amusement-parks.fun` : destinataire interne des notifications contact et meteo, sans usage automatique comme expediteur.

### Seed admin initial, optionnel

| Secret | Exemple / note |
|---|---|
| `PROD_ADMIN_USER_EMAIL` | uniquement si `PROD_ADMIN_USER_ENABLED=true` |
| `PROD_ADMIN_USER_PASSWORD` | mot de passe initial très fort |

Recommandation : activer le seed admin seulement au premier déploiement, puis repasser `PROD_ADMIN_USER_ENABLED=false`.

## Variables GitHub recommandées

### Activation du déploiement

| Variable | Valeur recommandée |
|---|---|
| `PRODUCTION_DEPLOY_ENABLED` | `false` tant que DNS/NPM/secrets ne sont pas prêts, puis `true` |
| `VPS_DEPLOY_PATH` | `/opt/amusementpark` |

### Domaine public

| Variable | Valeur recommandée |
|---|---|
| `PUBLIC_DOMAIN` | `amusement-parks.fun` |
| `PUBLIC_BASE_URL` | `https://amusement-parks.fun` |
| `PUBLIC_WWW_BASE_URL` | `https://www.amusement-parks.fun` |
| `PUBLIC_HTTP_PORT` | `18080` |

### Docker / réseaux

| Variable | Valeur recommandée |
|---|---|
| `NPM_DOCKER_NETWORK_NAME` | `nginx-proxy-network` sur ton VPS actuel |
| `BACKEND_PRIVATE_SUBNET` | `172.30.31.0/24` |
| `FORWARDED_HEADERS_KNOWN_NETWORKS` | `172.30.31.0/24` |
| `FORWARDED_HEADERS_ALLOWED_HOSTS` | `amusement-parks.fun;www.amusement-parks.fun;localhost;127.0.0.1` |
| `ALLOWED_HOSTS` | `amusement-parks.fun;www.amusement-parks.fun;localhost;127.0.0.1;api;amusementpark-api` |
| `SSR_ALLOWED_HOSTS` | `amusement-parks.fun;www.amusement-parks.fun;localhost;127.0.0.1` |
| `SSR_FORCE_HTTPS` | `true` |
| `SSR_CSP_ALLOW_LOCAL_DEV_SOURCES` | `false` |

### MinIO ports privés loopback

| Variable | Valeur recommandée |
|---|---|
| `MINIO_API_PORT` | `19000` |
| `MINIO_CONSOLE_PORT` | `19001` |
| `PROD_MINIO_BUCKET` | `amusement-park-images` |

### SEO / sitemap seed

| Variable | Valeur recommandée |
|---|---|
| `SEO_MAX_DYNAMIC_URLS_PER_TYPE` | `50` pour M19/M20, optimisation gros volume plus tard |

### CSP

| Variable | Valeur recommandée |
|---|---|
| `CSP_ENABLED` | `true` |
| `CSP_REPORT_ONLY` | `true` jusqu’à reprise M18.5 |

### OAuth redirects

| Variable | Valeur recommandée |
|---|---|
| `PROD_GOOGLE_REDIRECT_URI` | `https://amusement-parks.fun/api/auth/external/google/callback` |

### JWT non secrets

| Variable | Valeur recommandée |
|---|---|
| `PROD_JWT_ISSUER` | `AmusementPark` |
| `PROD_JWT_AUDIENCE` | `AmusementPark` |

### Email

| Variable | Valeur recommandée |
|---|---|
| `PROD_EMAIL_MODE` | `Smtp` en production ; `Console` uniquement pour un test volontaire sans vrais utilisateurs |
| `PROD_EMAIL_PORT` | `587` |
| `PROD_EMAIL_USE_SSL` | `false` |
| `PROD_EMAIL_USE_STARTTLS` | `true` |
| `PROD_EMAIL_FROM_ADDRESS` | `noreply@amusement-parks.fun` |
| `PROD_EMAIL_FROM_NAME` | `Amusement Park` |
| `PROD_EMAIL_NOTIFICATION_ADMIN_ADDRESS` | `admin@amusement-parks.fun` |
| `PROD_EMAIL_CONTACT_ADDRESS` | `contact@amusement-parks.fun` |
| `PROD_EMAIL_CONTACT_NOTIFICATIONS_ENABLED` | `true` |
| `PROD_EMAIL_WEATHER_NOTIFICATIONS_ENABLED` | `true` |

### Seed admin

| Variable | Valeur recommandée |
|---|---|
| `PROD_ADMIN_USER_ENABLED` | `true` uniquement au premier déploiement si besoin, puis `false` |

### Sauvegarde avant déploiement

| Variable | Valeur recommandée |
|---|---|
| `BACKUP_BEFORE_DEPLOY` | `true` |

### Migrations MongoDB de déploiement

| Variable | Valeur recommandée |
|---|---|
| `RUN_OPENING_HOURS_LOCALIZED_NOTES_MIGRATION` | `true` |
| `OPENING_HOURS_LOCALIZED_NOTES_MIGRATION_DRY_RUN` | `false` |

## Génération locale d’une clé JWT

Exemple PowerShell :

```powershell
[Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

## Remarques importantes

- Ne pas mettre de secrets dans GitHub Variables : utiliser GitHub Secrets.
- Ne pas réutiliser les mots de passe de la préprod locale.
- Si le VPS pull des images GHCR privées, `GHCR_USERNAME` et `GHCR_TOKEN` sont nécessaires.
- `CSP_REPORT_ONLY=true` doit rester actif pour le premier déploiement réel. Le passage enforce est M18.5.
