# Phase 4 - Correctif de démarrage minimal

## Objectif

Permettre au projet `AmusementPark.WebAPI` de démarrer après l'introduction de la couche `AmusementPark.Application`, sans imposer la migration complète de tous les ports et handlers dès cette étape.

## Choix appliqué

- `AddApplication()` passe en mode **minimal**.
- Les validateurs transverses restent enregistrés.
- Les handlers `ICommandHandler<,>` / `IQueryHandler<,>` ne sont **plus auto-enregistrés** par défaut.
- Une méthode `AddApplicationHandlers(...)` est conservée pour activer explicitement des handlers au fur et à mesure du branchement des adapters Infrastructure vers le legacy.

## Pourquoi

Les handlers de phase 4 dépendent de ports Application (`IParkRepository`, `IParkZoneRepository`, `ISearchReadRepository`, etc.).
Sans adapters Infrastructure déjà en place, l'auto-enregistrement complet casse la validation du conteneur DI au démarrage.

## Étape suivante recommandée

Pour chaque feature migrée :

1. créer l'adapter Infrastructure vers le service/repository legacy ;
2. enregistrer le port Application correspondant ;
3. activer explicitement le ou les handlers concernés via `AddApplicationHandlers(...)`.
