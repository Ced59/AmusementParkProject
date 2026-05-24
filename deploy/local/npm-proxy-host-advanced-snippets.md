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

## Proxy Host Matomo `matomo.amusement.localhost`

Matomo valide l'origine des formulaires. En local, comme NPM écoute sur le port `18080`, il faut préserver le host avec son port.

```nginx
proxy_set_header Host $http_host;
proxy_set_header X-Forwarded-Host $http_host;
proxy_set_header X-Forwarded-Proto $scheme;
proxy_set_header X-Forwarded-Port $server_port;
```

Le Proxy Host doit cibler :

```txt
Scheme: http
Forward Hostname / IP: matomo
Forward Port: 80
Websockets Support: enabled
```

Ne pas activer **Force SSL** sur ce Proxy Host local tant qu'aucun certificat local n'est configuré dans NPM.
