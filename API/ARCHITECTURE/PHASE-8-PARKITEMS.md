# Phase 8 - ParkItems

## Objectif

Migrer la feature `ParkItems` dans la Clean Architecture tout en supprimant la dépendance au `ParkItemsService` legacy.

## Périmètre implémenté

- handlers Application pour :
  - `GetParkItemsByParkId`
  - `GetParkItemsPage`
  - `GetParkItemById`
  - `CreateParkItem`
  - `UpdateParkItem`
  - `DeleteParkItem`
- validations de références éclatées dans `ParkItemReferenceValidator`
- normalisation métier éclatée dans `ParkItemNormalization`
- projection search resynchronisée sur create/update/delete
- enrichissement de la liste paginée admin avec `ParkName`
- exposition HTTP via `AmusementPark.WebAPI/Controllers/ParkItemsController.cs`

## Points d'attention couverts

- l'update recharge l'entité existante pour préserver l'identité et `CreatedAtUtc`
- les détails et localisations d'attraction sont normalisés hors du contrôleur
- la logique n'est plus concentrée dans une seule classe tentaculaire
