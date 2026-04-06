# Feature slices

## Convention

Chaque feature migrée doit suivre une structure homogène :

- `Commands/`
- `Queries/`
- `Handlers/`
- `Results/`
- `Validators/`
- `Ports/`

## Features cibles

- `Countries`
- `ParkFounders`
- `ParkOperators`
- `AttractionManufacturers`
- `Users`
- `Parks`
- `ParkZones`
- `ParkItems`
- `Images`
- `Search`
- `CaptainCoaster`

## Règles

- une feature ne dépend pas d'un contrôleur HTTP
- une feature ne dépend pas d'un document Mongo
- les modèles web restent dans `AmusementPark.WebAPI`
- les modèles BSON restent dans `AmusementPark.Infrastructure`
- les handlers applicatifs manipulent des résultats applicatifs, pas des `IActionResult`
- les handlers simples peuvent être implémentés avant les adapters infrastructure, tant qu'ils ne dépendent que de ports applicatifs
