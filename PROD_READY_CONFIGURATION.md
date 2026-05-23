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
