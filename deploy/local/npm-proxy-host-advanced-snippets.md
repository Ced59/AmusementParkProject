# Nginx Proxy Manager — snippets avancés local-prod

Ces snippets sont utiles uniquement pour l'environnement local proche production.
Ils se collent dans l'onglet **Advanced** du Proxy Host NPM concerné.

## Proxy Host applicatif `amusement.localhost`

Normalement aucun snippet n'est nécessaire. Le Proxy Host doit cibler :

```txt
Scheme: http
Forward Hostname / IP: front
Forward Port: 4000
Websockets Support: enabled
```

## Matomo local

L'administration Matomo locale doit de préférence se faire directement via :

```txt
http://localhost:18082
```

Ce choix évite les erreurs CSRF Matomo provoquées par un reverse proxy HTTP local sur port non standard.

Le Proxy Host `matomo.amusement.localhost` n'est donc plus recommandé pour l'administration Matomo locale. Il peut être supprimé ou ignoré.

Si tu veux tout de même expérimenter Matomo derrière NPM, le Proxy Host doit cibler :

```txt
Scheme: http
Forward Hostname / IP: matomo
Forward Port: 80
Websockets Support: enabled
Force SSL: disabled
```

Avec ce snippet Advanced :

```nginx
proxy_set_header Host $http_host;
proxy_set_header X-Forwarded-Host $http_host;
proxy_set_header X-Forwarded-Proto $scheme;
proxy_set_header X-Forwarded-Port $server_port;
```
