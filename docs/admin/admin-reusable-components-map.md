# Admin reusable components map

M25 reference les briques admin deja disponibles avant de creer le workbench.

## Reutiliser tel quel

- `features/admin/shared/ui/admin-json-import-tab`: import JSON localise deja isole et reutilisable dans les ecrans d'edition.
- `features/admin/shared/ui/admin-reference-images`: affichage d'images de reference pour comparer ou enrichir une entite.
- `features/admin/park-items/mappers`: options categorie/type et mapping du formulaire complet vers `ParkItem`.
- `features/admin/park-items/state`: facades et ports pour edition, index, zones, constructeurs, photos et locations.
- `features/admin/parks/state/admin-park-items-state.facade.ts`: pattern de liste park-scoped avec filtres, pagination, tri et bulk administration.

## Adapter prudemment

- `features/admin/park-items/pages/admin-park-items-index`: bonne base pour liste filtree et bulk, mais le workbench doit rester un flux dedie.
- `features/admin/parks/pages/admin-parks/admin-park-items`: utile pour le contexte parc, mais ne doit pas recevoir toute l'orchestration rapide.
- Les composants de tabs d'edition complete: reutilisables pour enrichissement, pas pour la saisie en rafale.

## Ne pas forcer

- Les formulaires complets de park item ne doivent pas devenir le workbench.
- Les DTO complets ne doivent pas absorber les champs rapides ou inline.
- Les services API concrets ne doivent pas etre injectes directement dans les composants du workbench.

## Cible M25

- `features/admin/park-items/workbench` contient les modeles, mappers et facades du flux rapide.
- `components/admin/park-items/workbench` reste reserve aux composants presentational reutilisables quand M26 ajoutera l'ecran.
