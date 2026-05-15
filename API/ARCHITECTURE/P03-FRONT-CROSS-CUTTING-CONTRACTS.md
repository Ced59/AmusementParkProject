# P03 — Fondation de l'architecture front — contrats transverses

## Objectif

Créer les briques transverses officielles qui éviteront aux futures phases de recréer localement
leurs propres contrats de pagination, filtre, tri, état d'écran et mapping léger.

P03 reste volontairement **iso-fonctionnelle** : il ne refond pas encore les features métier,
mais il pose le socle commun sur lequel P04 à P14 pourront s'appuyer.

## Périmètre de P03

- définition des contrats paginés communs ;
- définition des types partagés pour listes, filtres, tris et états d'écran ;
- définition des wrappers de résultat et d'erreur génériques ;
- ajout de helpers de mapping transverses ;
- conservation d'une compatibilité avec les modèles historiques déjà utilisés par l'application.

## Emplacements officiels

```text
src/app/shared/models/contracts/
src/app/shared/utils/mapping/
```

## Contrats officiels introduits

### Pagination et collections

- `PaginationContract`
- `CollectionResponse<TItem>`
- `PagedResult<TItem>`

### Requêtes de liste

- `ListQuery<TSortField, TFilterKey>`
- `SortDefinition<TField>`
- `FilterDefinition<TKey>`

### Erreurs et résultats

- `ApiError`
- `OperationResult<TData, TError>`

### États d'écran

- `ScreenState<TData, TError>`

## Convention de mapping

Les petits helpers transverses vivent dans `shared/utils/mapping` :

- `coalesceArray`
- `mapArray`
- `mapNullable`
- `normalizePagination`
- `createPagedResult`
- `mapCollectionResponse`

Ces helpers ne remplacent pas les mappers métier des features. Ils fournissent seulement
les briques communes pour éviter les répétitions de bas niveau.

## Compatibilité avec l'existant

Les fichiers historiques de `src/app/models/shared/` restent présents pour ne pas casser le code actuel.
Ils deviennent des points de compatibilité alignés sur les nouveaux contrats officiels de P03.

## Règles à appliquer à partir de P03

- toute nouvelle réponse paginée réutilise `CollectionResponse<TItem>` ou `PagedResult<TItem>` ;
- toute nouvelle requête de liste réutilise `ListQuery` et ses types associés ;
- les états d'écran transverses doivent converger vers `ScreenState` ;
- on évite de redéfinir des helpers `map`, `null -> []` ou `pagination fallback` dans chaque feature.

## Critères de fin

- un socle transverse officiel existe dans le code ;
- les conventions sont lisibles sans dépendre uniquement de la roadmap ;
- les futures phases peuvent s'appuyer sur des contrats communs au lieu d'inventer des formes locales.
