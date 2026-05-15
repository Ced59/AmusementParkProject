# shared/models/contracts

Contrats transverses officiels introduits par la phase **P03**.

Ils servent de socle commun pour :

- les réponses paginées ;
- les requêtes de liste, filtres et tris ;
- les états d'écran ;
- les wrappers d'erreur et de résultat.

## Contrats à utiliser en priorité

- `PaginationContract`
- `CollectionResponse<TItem>`
- `PagedResult<TItem>`
- `ListQuery<TSortField, TFilterKey>`
- `SortDefinition<TField>`
- `FilterDefinition<TKey>`
- `ScreenState<TData, TError>`
- `ApiError`
- `OperationResult<TData, TError>`

## Règle

Quand une nouvelle feature ou un nouveau service a besoin d'une pagination, d'un filtre, d'un tri
ou d'un état d'écran générique, on réutilise ces contrats au lieu de recréer des formes locales.
