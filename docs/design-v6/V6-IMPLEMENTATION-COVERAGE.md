# V6 — couverture d’implémentation

## Référence immuable

- Branche : `feat/ui-v6-playground-redesign`
- Base `origin/master` : `7f6a9088cb20e1fa6e450b2e8f4bacb9e2019a22`
- Date de la base : `2026-10-10T02:07:19+02:00`
- Version de départ : `5.4.101`
- Cible unique : `6.0.0`
- Référence graphique : `APF-Playground-V6-Accueil-Reel-Standalone.html` du pack V6 daté du 14 octobre 2026.
- Exceptions graphiques : le wordmark réel `AMUSEMENT-PARKS.fun` dans la navbar et le footer, puis les deux segments traduits du slogan du footer.

## Inventaire P00 automatisé

Le script `FRONT/AmusementPark/tools/design-v6/generate-p00-inventory.mjs` analyse `app.routes.ts`, les enfants `loadChildren`, les métadonnées des composants, leurs templates et leurs styles. Il doit être rejoué après chaque intégration de `origin/master` et avant P22.

| Élément | Quantité de référence |
|---|---:|
| Routes terminales, redirections et écrans développés | 148 |
| Composants Angular de présentation | 273 |
| Interactions et champs explicites | 1 869 |
| Feuilles SCSS globales et locales | 268 |
| Templates de route manquants | 0 |
| Écrans d’administration | 58 |
| Écrans compte/profil | 26 |
| Écrans publics généraux | 29 |
| Écrans parc | 19 |
| Écrans ParkItem | 10 |
| Écrans standalone | 3 |

Les sources exhaustives et versionnables sont :

- `generated/V6-ROUTE-INVENTORY.csv` : route, composant, template, SCSS, guards, resolvers et métriques d’état ;
- `generated/V6-COMPONENT-INVENTORY.csv` : tous les composants `ui`, `shared`, `features/public`, `features/profile`, `features/auth` et `features/admin` ;
- `generated/V6-INTERACTION-INVENTORY.csv` : boutons, liens, formulaires, champs, handlers, routes, états disabled et conditions de visibilité.
- `generated/V6-STYLE-INVENTORY.csv` : taille, variables, media/container queries, gradients, ombres, rayons et usages de `!important` de chaque feuille SCSS.

Deux lignes `/:lang` peuvent apparaître dans la matrice : elles appartiennent aux deux branches de layout ordonnées de l’application. Elles ne représentent pas deux URLs publiques distinctes.

## Gates

| Gate | État | Preuve attendue |
|---|---|---|
| P00 — baseline et inventaires | PASS | 4 CSV générés, 24 captures V5, baseline performance/SSR, cas protégés |
| P01 — tokens et thèmes | PASS | mapping documenté, 4 tests ciblés, moteur SSR intact |
| P02 — primitives | PASS | 7 tests statiques verts, 160 facades conformes, suite a11y complète réservée à P21 |
| P03 — shells et navigation | À FAIRE | wordmark/slogan protégés, contrôles inchangés |
| P04–P16 — parcours publics | À FAIRE | toutes les familles de routes et données réelles |
| P17 — auth/compte | À FAIRE | endpoints, validations et permissions inchangés |
| P18 — administration | À FAIRE | 58 écrans + workflows denses couverts |
| P19 — états rares | À FAIRE | modales, toasts, consentement, erreurs, Quill |
| P20 — responsive/a11y | À FAIRE | matrice 320–1920, zoom, clavier, deux thèmes |
| P21 — CI/performance/SEO | À FAIRE | scripts contractuels verts |
| P22 — version et release atomique | À FAIRE | 6.0.0, PR unique, feu vert explicite |

Un gate reste bloquant tant que sa preuve n’est pas enregistrée. La version et la PR ne seront préparées qu’à P22.

## Registre des cas sensibles P00

| Domaine | Référence conservée | Preuve de comportement attendue |
|---|---|---|
| Leaflet | composants `ui/maps` et cartes publiques | bounds, zoom, drag, tap, marqueurs, popup, filtre, navigation |
| Plans officiels | routes parc maps et sélecteur d’année | image/source/année, plans multiples, absence de plan |
| Médias | `app-image-display`, galerie, carrousel, vidéos | 0/1/8/9/100+, lazy, alt, crédits, erreur réseau, lightbox |
| Historique | timeline, article, snapshot, comparaison | pagination, années, sources, deep link, SEO SSR |
| Météo/horaire/prix/live | composants pratiques du parc/standalone | fuseau, fraîcheur, indisponible, mois, canaux et sources |
| Notes/commentaires | ratings et commentaires publics | anonyme/connecté, seuils, langue, pagination, erreurs |
| Collections/watch/passeport | routes publiques et profil | login, permissions, confirmation, état vide/plein |
| Park Fit/trips | parcours conditionnels | validations, retour, comparaison, brouillon local |
| Édition/import admin | formulaires, workbench, field mode | dirty, preview/apply, upload, long job, erreur et annulation |
| Auth/compte | modales et routes profil | login, Google, inscription, reset, expiration, export/suppression |

Les 58 terminaux admin et les 26 terminaux compte sont explicitement présents dans la matrice de routes ; ils ne peuvent pas être exclus par une absence de maquette.

## Invariants fonctionnels

- Aucun changement d’API, DTO, facade, port, guard, resolver, route, slug, query parameter ou règle de visibilité.
- Aucun changement d’authentification, rôle, pagination serveur, classement, note, collection, watch, passeport, Park Fit, trip ou workflow admin.
- Aucun remplacement de Leaflet, Quill, SSR, hydratation, `TransferState`, `@defer`, lazy loading, cache, SEO, canonical, hreflang ou JSON-LD.
- Aucun nombre, parc, ParkItem, prix, horaire, météo ou événement de la maquette n’est codé en dur.
- Les pages ParkItem restent des pages complètes ; les attractions standalone restent autonomes.
- Les écrans admin gardent leur densité et leurs opérations, sans entrer dans le bundle public initial.

