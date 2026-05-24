# Première mise en production — CI/CD et VPS

Ce document décrit le déploiement cible après validation de l’environnement local proche production M20.

## Architecture cible sur le VPS

Le VPS possède déjà Nginx Proxy Manager. Le stack AmusementPark **ne redéploie pas NPM** en production.

```text
Internet
  -> Nginx Proxy Manager existant : 80 / 443 / 81
  -> 127.0.0.1:${PUBLIC_HTTP_PORT:-18080}
  -> container Angular SSR : front:4000
  -> API privée Docker : api:8080
  -> MongoDB privé Docker
  -> MinIO privé Docker
```

Décisions retenues après les tests local-prod-like :

- front de prod = **Angular SSR Node**, pas Nginx statique ;
- API non exposée publiquement ;
- MongoDB non exposé publiquement ;
- MinIO exposé seulement sur `127.0.0.1` pour tunnel SSH / maintenance ;
- ports hôte non standards par défaut pour réduire les conflits avec les services déjà présents ;
- CSP locale retirée de la configuration prod SSR ;
- `SSR_ALLOWED_HOSTS` obligatoire pour éviter les erreurs SSR Angular `allowedHosts` vues en local ;
- `SSR_FORCE_HTTPS=true` en filet de sécurité derrière le `Force SSL` NPM.

## Ports par défaut production

| Service | Port hôte | Exposition |
|---|---:|---|
| Front SSR via NPM | `127.0.0.1:18080` | local VPS seulement |
| MinIO API | `127.0.0.1:19000` | local VPS / tunnel SSH |
| MinIO Console | `127.0.0.1:19001` | local VPS / tunnel SSH |
| API | aucun port hôte | Docker privé |
| MongoDB | aucun port hôte | Docker privé |

## Commandes de diagnostic à exécuter sur le VPS

Avant le premier déploiement, connecte-toi au VPS puis exécute :

```bash
mkdir -p /opt/amusementpark
cd /opt/amusementpark
```

Si le bundle de déploiement a déjà été envoyé par la pipeline, lance :

```bash
./scripts/vps-preflight.sh
```

Sinon, pour collecter les informations utiles avant même la pipeline :

```bash
uname -a
lsb_release -a || cat /etc/os-release
df -h /
free -h
docker --version
docker compose version
docker ps --format 'table {{.Names}}\t{{.Image}}\t{{.Status}}\t{{.Ports}}'
ss -ltnp | grep -E ':(80|81|443|18080|19000|19001)\b' || true
docker network ls
docker network inspect $(docker network ls -q) --format '{{.Name}} {{range .IPAM.Config}}{{.Subnet}} {{end}}' 2>/dev/null || true
```

Ces commandes sont en lecture seule.

## Nginx Proxy Manager production

À faire après le premier déploiement Docker, lorsque le domaine pointera vers le VPS :

```text
Domain Names: amusement-parks.fun, www.amusement-parks.fun
Scheme: http
Forward Hostname / IP: 127.0.0.1
Forward Port: 18080
Websockets Support: enabled
SSL Certificate: Let's Encrypt
Force SSL: enabled
HTTP/2 Support: enabled
```

Ne crée pas de proxy host public pour l’API. L’API doit rester accessible via :

```text
https://amusement-parks.fun/api/...
```

## Déclenchement pipeline

La CI tourne sur :

- pull request vers `main` ou `master` ;
- push vers `main` ou `master` ;
- lancement manuel `workflow_dispatch`.

Le déploiement VPS se lance uniquement si :

- push sur `main` ou `master` **et** variable GitHub `PRODUCTION_DEPLOY_ENABLED=true` ;
- ou lancement manuel avec input `deploy=true`.

Ce garde-fou évite un déploiement accidentel avant que DNS, secrets et NPM soient prêts.

## Images Docker

La pipeline pousse :

```text
ghcr.io/<owner>/amusementpark-api:<sha>
ghcr.io/<owner>/amusementpark-front:<sha>
```

Le VPS pull ces tags immuables depuis le `.env` généré par GitHub Actions.

Si les packages GHCR sont privés, renseigner `GHCR_USERNAME` et `GHCR_TOKEN` dans les secrets GitHub.

## Smoke tests exécutés par `deploy.sh`

Après `docker compose up -d`, le script vérifie :

```bash
GET /healthz
GET /api/health
GET /robots.txt
```

avec les headers :

```text
Host: amusement-parks.fun
X-Forwarded-Proto: https
```

Ça évite les faux résultats liés à `SSR_FORCE_HTTPS=true`.

## Après premier déploiement réel

À vérifier dans cet ordre :

1. NPM Force SSL actif.
2. `http://amusement-parks.fun` redirige en `https://amusement-parks.fun`.
3. `https://amusement-parks.fun/en/home` renvoie du HTML SSR.
4. `/robots.txt` et `/sitemap.xml` sont accessibles en HTTPS.
5. Login classique admin.
6. Google OAuth après ajout des origines/redirects dans Google Cloud.
7. Upload image / MinIO.
8. Import Captain Coaster si besoin.
9. Matomo prod : `matomo.js` + `matomo.php` après consentement optionnel.
10. Logs CSP Report-Only.

M18.5 reste volontairement différé : ne passer CSP en enforce qu’après observation des rapports sur le vrai domaine HTTPS.
