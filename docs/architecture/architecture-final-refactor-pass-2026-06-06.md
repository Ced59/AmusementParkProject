# AmusementPark — passe finale refacto architecture front/back — 2026-06-06

Cette passe complète les deux livraisons précédentes en ciblant les derniers points remontés par l'audit : dépendance directe des facades Angular aux services concrets, manque de tests sur l'orchestration publique, et fichiers d'orchestration encore trop concentrés.

## Front Angular

### Captain Coaster comparison

Le fichier `captain-coaster-comparison.facade.ts` ne porte plus la normalisation API ni la construction des résolutions de doublons.

Fichiers ajoutés :

- `features/admin/data/state/captain-coaster-comparison-normalizer.ts`
- `features/admin/data/state/captain-coaster-duplicate-resolution.helpers.ts`
- `features/admin/data/state/captain-coaster-comparison-data.port.ts`
- `features/admin/data/state/captain-coaster-comparison-normalizer.spec.ts`
- `features/admin/data/state/captain-coaster-duplicate-resolution.helpers.spec.ts`

Effets :

- la facade repasse sous un seuil plus raisonnable ;
- la normalisation PascalCase/camelCase est testée séparément ;
- les règles de résolution de doublons deviennent de la logique pure testable ;
- la facade dépend désormais d'un port `CaptainCoasterComparisonDataPort` plutôt que de `DataSourcesApiService` directement.

### Park item detail

La facade publique `ParkItemDetailStateFacade` a été alignée sur `ParkDetailStateFacade`.

Fichiers ajoutés :

- `features/public/park-items/state/park-item-detail-data.ports.ts`
- `features/public/park-items/state/park-item-detail-state.facade.spec.ts`

Effets :

- dépendances remplacées par des ports Angular `InjectionToken` ;
- tests ajoutés sur l'orchestration principale ;
- test du cas 404 SSR ;
- test des fallbacks optionnels quand photos, tags, related items, manufacturer ou zone échouent.

## Back .NET

### ParkGraphUpsertProcessor

Le processor principal ne contient plus les gros blocs `ProcessFounders`, `ProcessOperators`, `ProcessManufacturers`, `ProcessZones`, `ProcessItems` et `ProcessImages`.

Fichiers ajoutés :

- `ParkGraphUpsertProcessor.References.cs`
- `ParkGraphUpsertProcessor.Zones.cs`
- `ParkGraphUpsertProcessor.Items.cs`
- `ParkGraphUpsertProcessor.Images.cs`

Effets :

- `ParkGraphUpsertProcessor.cs` garde principalement le constructeur, `PreviewAsync`, `ApplyAsync`, `ProcessAsync` et l'historisation ;
- les traitements par famille d'entité sont isolés ;
- le refactor reste conservateur : aucune signature publique changée, logique métier déplacée sans réécriture fonctionnelle.

## Validations exécutées

Depuis `FRONT/AmusementPark` :

```bash
./node_modules/.bin/tsc -p tsconfig.app.json --noEmit
./node_modules/.bin/tsc -p tsconfig.spec.json --noEmit
npm run build -- --configuration development --progress=false
```

Résultat : OK.

## Limite restante

La compilation .NET n'a pas pu être exécutée dans cet environnement parce que le SDK `dotnet` n'est pas installé. La passe backend est donc volontairement une extraction `partial` conservative, sans changement de comportement intentionnel.

Les derniers fichiers volumineux restants sont surtout côté infrastructure (`CaptainCoasterDataSourceProvider.*`, repositories Mongo, documents Mongo). Ils sont moins prioritaires que les handlers/facades car ils sont déjà localisés côté infrastructure et ne contaminent pas les couches hautes.
