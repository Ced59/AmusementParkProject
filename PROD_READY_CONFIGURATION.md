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

## Règle de sélection du sender mail

- `Email:Mode=Console` => `ConsoleEmailSender`
- `Email:Mode=Smtp` => `SmtpEmailSender`

## Fichier d'exemple

Un fichier `API/WebAPI/appsettings.Production.example.json` est fourni comme base de configuration.
