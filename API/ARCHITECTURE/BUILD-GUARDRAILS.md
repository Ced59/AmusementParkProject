# Build guardrails

Les garde-fous de phase 2 sont implémentés au niveau MSBuild via :

- `API/Directory.Build.props`
- `API/Directory.Build.targets`

## Ce qui est vérifié

- les nouveaux projets `AmusementPark.*` gardent les bonnes références inter-projets
- `Core` ne prend pas de références projet métier ni de packages techniques interdits
- `Application` ne dérive pas vers Mongo, ASP.NET Core ou d'autres détails d'infra
- `WebAPI` ne référence pas directement `Core` ni les anciens projets legacy

## Portée

Les vérifications ne s'appliquent qu'aux projets `AmusementPark.*`.
Les anciens projets legacy ne sont pas cassés par cette phase.

## But

Faire échouer tôt le build si quelqu'un réintroduit une dépendance illégitime dans la nouvelle architecture.
