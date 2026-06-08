# M18.3 — Durcissement Forwarded Headers

## Objectif

L'API ne doit plus accepter les en-têtes `X-Forwarded-*` venant de n'importe quelle adresse.
Ces en-têtes influencent notamment :

- l'adresse IP cliente vue par ASP.NET Core ;
- le scheme utilisé pour les redirections HTTPS ;
- le host utilisé par les URLs générées et les callbacks.

## Décision appliquée

La configuration précédente vidait les réseaux/proxys connus (`KnownNetworks`/`KnownProxies`), ce qui revenait à faire confiance à tout émetteur de headers forwarded.

La configuration M18.3 applique désormais :

- `X-Forwarded-For`, `X-Forwarded-Proto` et `X-Forwarded-Host` uniquement via le middleware ASP.NET Core ;
- `KnownProxies` explicites pour les boucles locales nécessaires aux healthchecks ;
- `KnownIPNetworks` ASP.NET Core, alimenté par la configuration applicative `ForwardedHeaders:KnownNetworks`, pour le réseau Docker backend autorisé ;
- `AllowedHosts` dédiés aux valeurs acceptées de `X-Forwarded-Host` ;
- `ForwardLimit` configurable, par défaut à `2` pour couvrir la chaîne Nginx Proxy Manager -> front Nginx -> API.

## Configuration production recommandée

```bash
PUBLIC_EDGE_SUBNET=172.30.30.0/24
BACKEND_PRIVATE_SUBNET=172.30.31.0/24
FORWARDED_HEADERS_KNOWN_NETWORKS=172.30.31.0/24
FORWARDED_HEADERS_ALLOWED_HOSTS=amusement-parks.fun;www.amusement-parks.fun;localhost;127.0.0.1
FORWARDED_HEADERS_FORWARD_LIMIT=2
```

Le réseau réellement utilisé par l'API est `backend_private`, car le front SSR appelle l'API via le service Docker `http://api:8080/` sur ce réseau. Ce host interne doit rester présent dans `ALLOWED_HOSTS`.

## Cas Nginx Proxy Manager externe

Si Nginx Proxy Manager est dans un autre container Docker et que sa propre IP apparaît dans la chaîne `X-Forwarded-For`, ajouter son réseau Docker à `FORWARDED_HEADERS_KNOWN_NETWORKS`, par exemple :

```bash
FORWARDED_HEADERS_KNOWN_NETWORKS=172.30.31.0/24;172.18.0.0/16
```

Le parser accepte les séparateurs `;` et `,` pour faciliter l'injection via `.env`.

## Validation attendue

- Une requête directe vers l'API avec des headers `X-Forwarded-*` falsifiés est ignorée si elle ne vient pas d'un proxy/réseau connu.
- Les healthchecks internes continuent de fonctionner.
- Les redirections HTTPS restent cohérentes derrière Nginx Proxy Manager.
- `X-Forwarded-Host` n'est accepté que pour les hosts explicitement configurés.
