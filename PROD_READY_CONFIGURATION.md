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

## Règle de sélection du sender mail

- `Email:Mode=Console` => `ConsoleEmailSender`
- `Email:Mode=Smtp` => `SmtpEmailSender`

## Fichier d'exemple

Le fichier `deploy/.env.production.example` est fourni comme base de configuration pour le déploiement Docker/VPS.
