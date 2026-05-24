# UI Maps

Composants visuels cartographiques issus de la phase M05.

- `app-ui-map-shell` fournit la structure responsive.
- `app-ui-map-slot` héberge une vraie carte ou un état placeholder.
- `app-ui-distance-panel` affiche des distances/métriques déjà calculées ailleurs.
- `leaflet/` centralise les marqueurs Leaflet personnalisés du site.
- Le choix métier d'un marqueur reste porté par `MapMarker.iconKind` et les resolvers purs de `shared/utils/maps`.
- Aucun calcul métier de distance n'est réalisé ici.
- Le style est centralisé dans `src/styles/_maps.scss`.
