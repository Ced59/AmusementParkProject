# Phase 6 — Features simples migrées de bout en bout

Features migrées dans la nouvelle stack Clean Architecture :

- Countries
- ParkFounders
- ParkOperators
- AttractionManufacturers

## Câblage réalisé

- Contrôleurs HTTP dédiés dans `AmusementPark.WebAPI`
- Mapping HTTP <-> domaine dans `AmusementPark.WebAPI/Mappers`
- Handlers Application explicitement enregistrés via `AddApplicationHandlers(...)`
- Repositories Mongo branchés dans `AmusementPark.Infrastructure`
- Projection Search remise en place pour `ParkOperators` et `AttractionManufacturers`
- Calcul de `AttractionCount` restauré pour `AttractionManufacturers`
- Tri de lecture réaligné sur le legacy pour les référentiels simples
- Messages d'erreur fonctionnels réalignés sur le contrat legacy pour les routes migrées

## Routes ciblées

- `GET /Countries`
- `GET /park-founders`
- `GET /park-founders/{id}`
- `POST /park-founders`
- `PUT /park-founders/{id}`
- `GET /park-operators`
- `GET /park-operators/{id}`
- `POST /park-operators`
- `PUT /park-operators/{id}`
- `GET /attraction-manufacturers`
- `GET /attraction-manufacturers/{id}`
- `POST /attraction-manufacturers`
- `PUT /attraction-manufacturers/{id}`

## Remarque de périmètre

La phase 6 reste volontairement limitée aux premières features verticales simples.
Les autres pans (Parks, ParkZones, ParkItems, Images, Users/Auth, Search complet, Captain Coaster) restent hors migration dans cette livraison.
