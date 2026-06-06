# AmusementPark — Finalisation du rollout architecture front/back — 2026-06-06

Cette passe termine le travail demandé après la réévaluation v2 : généralisation du DIP front, extension des tests d'orchestration et ajout de garde-fous pour éviter le retour des anciens points faibles.

## Front Angular

### DIP généralisé sur les facades

Toutes les facades qui injectaient directement un service `*ApiService` passent désormais par un port local + `InjectionToken`.

Objectif : conserver les facades comme couche d'orchestration de feature, testable avec des fakes capturants, sans dépendance directe aux classes concrètes `data-access`.

Exemples de ports ajoutés :

- `admin-parks-state-data.ports.ts`
- `admin-users-state-data.ports.ts`
- `home-state-data.ports.ts`
- `park-list-state-data.ports.ts`
- `captain-coaster-pipeline-data.ports.ts`
- `admin-park-item-photos-state-data.ports.ts`
- `park-reference-detail-state-data.ports.ts`

Un garde-fou exécutable a été ajouté :

```bash
npm run architecture:facade-ports
```

Il échoue si une facade importe à nouveau un `*ApiService` concret depuis `@data-access` / `@app/data-access`, ou si un constructeur réinjecte directement un `*ApiService` au lieu d'un port.

### Tests d'orchestration ajoutés

Les tests de facades passent de 2 à 9 fichiers dédiés.

Nouveaux tests :

- `admin-data-sources.facade.spec.ts`
- `admin-users-state.facade.spec.ts`
- `admin-parks-state.facade.spec.ts`
- `confirm-account-page-state.facade.spec.ts`
- `forgot-password-page-state.facade.spec.ts`
- `reset-password-page-state.facade.spec.ts`
- `home-state.facade.spec.ts`
- `park-list-state.facade.spec.ts`

Les tests utilisent des fakes manuels capturants pour vérifier :

- appels exacts aux ports ;
- fallbacks d'erreur ;
- conservation des données précédentes ;
- bascule empty / ready / error ;
- orchestration en cascade, par exemple hero parks puis featured parks sur la home.

### Validations front exécutées

```bash
npm run architecture:facade-ports
./node_modules/.bin/tsc -p tsconfig.app.json --noEmit
./node_modules/.bin/tsc -p tsconfig.spec.json --noEmit
npm run build -- --configuration development --progress=false
```

Résultat : OK.

## Back .NET

### Garde-fou anti-fichiers obèses

Ajout de `ApplicationSourceShapeTests` côté `AmusementPark.Application.Tests`.

Ces tests empêchent le retour des gros fichiers applicatifs :

- budget global fichier Application : 650 lignes ;
- budget spécifique handlers Application : 550 lignes.

Cela protège directement le refactor déjà réalisé sur :

- `ApplyLocalizedContentJsonCommandHandler.*`
- `ParkGraphUpsertProcessor.*`

### Test handler ajouté

Ajout de `GetParkByIdQueryHandlerTests` avec fake repository manuel.

Cas couverts :

- identifiant vide : échec sans appel repository ;
- identifiant avec espaces : trim avant appel repository ;
- parc introuvable : échec applicatif ;
- parc trouvé : succès avec entité retournée.

### Limite backend

Le SDK `dotnet` n'est pas disponible dans l'environnement d'exécution utilisé ici. Les changements backend ont donc été écrits de façon conservative et relus statiquement, mais non compilés localement.

## État après cette passe

- DIP front : généralisé sur les facades qui consomment `data-access`.
- Tests de facades : patron appliqué à plusieurs familles de features, plus seulement aux deux pages détail.
- Fichiers obèses : refactor déjà fait, désormais protégé par tests d'architecture côté Application.
- Build Angular : OK.
