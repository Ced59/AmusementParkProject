# AmusementPark — Architecture refactor pass — 2026-06-06

## Objectif

Cette passe ferme les principaux points de dette remontés par l'audit architecture front/back : fichiers trop volumineux, orchestration insuffisamment testée et compilation des tests TypeScript.

La règle générale reste inchangée : respecter l'architecture existante, SOLID, le typage explicite, les accolades systématiques, les facades côté front, les handlers/ports côté back, et privilégier l'extraction par responsabilité plutôt qu'une réécriture fonctionnelle.

## Front Angular

### Park detail mapper

Le mapper `park-detail-view.mapper.ts` a été réduit à un point d'entrée lisible. Les responsabilités sont maintenant séparées :

- `park-detail-info.mapper.ts` : lignes d'identité, infos pratiques, publication, localisation, statistiques.
- `park-detail-gallery.mapper.ts` : photos parc/items, catégories, hero image.
- `park-detail-mapping.model.ts` : contrats de mapping utilisés par la facade.

### Park item detail mapper

Le mapper `park-item-detail-view.mapper.ts` a été transformé en composition de sous-mappers :

- `park-item-detail-rows.mapper.ts` : lignes techniques, performance, expérience, résumé, spotlight.
- `park-item-detail-access.mapper.ts` : conditions d'accès et métriques de taille.
- `park-item-detail-location.mapper.ts` : points de localisation, marqueurs carte, centre carte.
- `park-item-detail-photos.mapper.ts` : galerie et catégories de photos.
- `park-item-detail-navigation.mapper.ts` : liens et query params de navigation.
- `park-item-detail-presentation.mapper.ts` : icônes, tons, clés de traduction métier.
- `park-item-detail-formatters.ts` : formatage pur.
- `park-item-detail-row.helpers.ts` : helpers de lignes de détail.
- `park-item-detail-related.mapper.ts` : suggestions d'items liés.

### Tests front

La compilation des specs TypeScript est corrigée :

- corrections des tests de services consentement/cookies avec `PLATFORM_ID` typé comme `object` ;
- correction des helpers de guards pour tenir compte du type Angular moderne `GuardResult` ;
- correction d'un test `CountriesApiService` dont l'inférence Jasmine partait sur `null` ;
- correction d'un cast volontaire dans les tests de conditions d'accès ;
- correction de la création de `File` dans le test de sécurité upload.

Validé par :

```bash
./node_modules/.bin/tsc -p tsconfig.app.json --noEmit
./node_modules/.bin/tsc -p tsconfig.spec.json --noEmit
npm run build -- --configuration development --progress=false
```

## Back .NET

### ParkGraphUpsertProcessor

`ParkGraphUpsertProcessor.cs` est découpé en partials par responsabilité :

- `ParkGraphUpsertProcessor.cs` : orchestration principale preview/apply.
- `ParkGraphUpsertProcessor.Patching.cs` : patch entités métier.
- `ParkGraphUpsertProcessor.Resolution.cs` : résolution/matching, changements, compteurs.
- `ParkGraphUpsertProcessor.JsonReading.cs` : lecture JSON typée.
- `ParkGraphUpsertProcessor.PrimitivePatching.cs` : patchs primitifs.
- `ParkGraphUpsertProcessor.LocalizedText.cs` : textes localisés et formatage.

### ApplyLocalizedContentJsonCommandHandler

Le handler localisé est découpé en partials :

- `ApplyLocalizedContentJsonCommandHandler.cs` : point d'entrée et dispatch.
- `ApplyLocalizedContentJsonCommandHandler.Entities.cs` : application par entité.
- `ApplyLocalizedContentJsonCommandHandler.AccessConditions.cs` : conditions d'accès.
- `ApplyLocalizedContentJsonCommandHandler.RawFields.cs` : champs bruts.
- `ApplyLocalizedContentJsonCommandHandler.FieldReaders.cs` : lecture/conversion de champs.
- `ApplyLocalizedContentJsonCommandHandler.Parsing.cs` : parsing JSON déjà extrait.

## Limites assumées

- Aucun changement fonctionnel volontaire n'a été introduit dans les pipelines d'import JSON.
- Le build/test .NET n'a pas pu être exécuté dans l'environnement local car le SDK `dotnet` n'est pas installé.
- Le build Angular développement SSR/browser passe. Le build production n'a pas été retenu comme validation finale ici pour éviter d'allonger inutilement le livrable, mais la compilation Angular complète en configuration développement a validé les templates et bundles.
