# Refonte Google Social Login - Notes d'intégration

## Ce qui change

- **Front** : abandon du flux OAuth `code` maison au profit de **Google Identity Services** en mode **popup**.
- **Back** : abandon de l'échange manuel `code -> access_token -> userinfo`.
- **Back** : validation directe du **Google ID token** avec la librairie `Google.Apis.Auth`.
- **Domaine User** : ajout d'une collection `ExternalLogins` sur l'utilisateur pour préparer la coexistence Google / Facebook / login local.
- **API** : nouvel endpoint `POST /auth/external/{provider}`.

## Endpoint utilisé par le front

`POST /auth/external/google`

Payload JSON :

```json
{
  "token": "GOOGLE_ID_TOKEN",
  "nonce": null
}
```

## Configuration Google Cloud Console à vérifier

Le nouveau flux **popup GIS** repose surtout sur les **Authorized JavaScript origins** du client OAuth Web.

### En local

Ajouter au minimum :

- `http://localhost:4200`

### En production

Ajouter le ou les domaines front réels :

- `https://votre-front.example.com`

### Important

Le flux popup n'a plus besoin du callback Angular `/signin-google`.

## Configuration back à vérifier

Dans la config back, la valeur importante pour la validation est :

- `Authentication:Google:ClientId`

Le `ClientSecret`, `RedirectUri`, `GrantType`, `TokenExchangeEndpoint` et `UserInfosEndpoint` ne sont plus utilisés par ce flux Google-là.

## Comportement métier ajouté

- login direct si le compte est déjà lié à Google ;
- création automatique si aucun compte n'existe ;
- auto-link par email **seulement** dans les cas raisonnablement sûrs :
  - compte legacy social sans mot de passe ni liens externes ;
  - ou email Google considéré comme autoritatif (`gmail.com`, `googlemail.com`, Workspace / `hd`).
- sinon retour d'un conflit pour éviter un rattachement risqué à un compte existant.

## Tests manuels recommandés

1. utilisateur Google jamais vu auparavant ;
2. utilisateur déjà créé par l'ancien système Google ;
3. utilisateur local existant avec email Gmail identique ;
4. utilisateur local existant avec email tiers identique ;
5. utilisateur bloqué ;
6. utilisateur désactivé ;
7. utilisateur déjà connecté puis logout puis reconnexion.

## Fichiers principaux touchés

### Back
- `API/WebAPI/Controllers/AuthController.cs`
- `API/WebAPI/Program.cs`
- `API/Services/Services/Implementations/Authentication/*`
- `API/Services/Services/Interfaces/IExternalAuthenticationService.cs`
- `API/Services/Services/Interfaces/Authentication/IExternalIdentityProviderService.cs`
- `API/Services/Services/Models/Authentication/VerifiedExternalIdentity.cs`
- `API/Entities/Entities/Model/Users/User.cs`
- `API/Entities/Entities/Model/Users/ExternalLogin.cs`
- `API/Entities/Entities/Model/Users/ExternalLoginProvider.cs`
- `API/Dtos/Dtos/Users/ExternalLogin/ExternalLoginRequestDto.cs`
- `API/Repositories/Repositories/Interfaces/IUserQueryHandler.cs`
- `API/Repositories/Repositories/Implementations/UsersMongoQueryHandler.cs`

### Front
- `FRONT/AmusementPark/src/app/components/login-register/auth-modal/*`
- `FRONT/AmusementPark/src/app/services/auth/google-identity.service.ts`
- `FRONT/AmusementPark/src/app/services/auth/auth.service.ts`
- `FRONT/AmusementPark/src/app/services/api.service.ts`
- `FRONT/AmusementPark/src/app/api/api-endpoints.ts`
- `FRONT/AmusementPark/src/app/interceptors/auth.interceptor.ts`
- `FRONT/AmusementPark/src/app/app-routing.module.ts`
- `FRONT/AmusementPark/src/app/app.module.ts`
- `FRONT/AmusementPark/src/app/app.component.ts`
- `FRONT/AmusementPark/src/types/google-identity.d.ts`
