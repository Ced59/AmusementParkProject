# État actuel du VPS avant première production

Diagnostic fourni le 2026-05-24.

## Points validés

- Docker est disponible : `Docker version 27.0.3`.
- Docker Compose est disponible : `v2.28.1`.
- Nginx Proxy Manager existe déjà et occupe `80`, `81`, `443`.
- Le VPS dispose d'environ `73G` libres sur `/`.
- La RAM disponible est suffisante pour un premier MVP : environ `4.8Gi` disponibles.
- Les ports prévus pour AmusementPark ne sont pas occupés dans le diagnostic : `18080`, `19000`, `19001`.

## Ajustement important retenu

Le Nginx Proxy Manager existant tourne dans Docker sur le réseau :

```text
nginx-proxy-network 172.19.0.0/16
```

Un conteneur NPM ne peut pas atteindre `127.0.0.1:18080` du VPS comme si c'était le host : dans Docker, `127.0.0.1` désigne le conteneur NPM lui-même.

La production est donc ajustée pour connecter le front SSR AmusementPark au réseau Docker externe existant :

```text
NPM_DOCKER_NETWORK_NAME=nginx-proxy-network
```

Dans NPM, le Proxy Host devra viser :

```text
Forward Hostname / IP: amusementpark-front
Forward Port: 4000
Scheme: http
```

Le port `127.0.0.1:18080` reste exposé uniquement comme port de diagnostic local sur le VPS.

## Réseaux existants observés

```text
bridge                  172.17.0.0/16
ranch-net               172.18.0.0/16
nginx-proxy-network     172.19.0.0/16
matomo_matomo_network   172.21.0.0/16
```

Le réseau privé AmusementPark reste par défaut :

```text
BACKEND_PRIVATE_SUBNET=172.30.31.0/24
```

Cette plage ne conflit pas avec les réseaux observés.

## NPM production à configurer plus tard

Après déploiement et DNS :

```text
Domain Names: amusement-parks.fun, www.amusement-parks.fun
Scheme: http
Forward Hostname / IP: amusementpark-front
Forward Port: 4000
Websockets Support: enabled
SSL Certificate: Let's Encrypt
Force SSL: enabled
HTTP/2 Support: enabled
```

Ne pas créer de Proxy Host direct vers `amusementpark-api`.
