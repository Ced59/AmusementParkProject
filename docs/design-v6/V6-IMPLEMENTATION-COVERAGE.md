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
| P03 — shells et navigation | PASS | wordmark/slogan protégés, 17 tests statiques, contrats de navigation inchangés |
| P04 — accueil | PASS | structure réelle complète, 24 tests statiques, données et recherche dynamiques |
| P05 — catalogue, exploration, recherche | PASS | filtres, carte, URL et pagination conservés ; 28 tests statiques cumulés |
| P06 — détail de parc | PASS | toutes les sections/actions conservées, 32 tests statiques cumulés |
| P07 — inventaire des ParkItems | PASS | filtres, carte, états sans coordonnées et pagination conservés ; 35 tests statiques cumulés |
| P08 — fiches ParkItem | PASS | navigation, sections techniques, médias et actions conservées ; 35 tests statiques cumulés |
| P09 — attractions autonomes | PASS | parent autonome, historique, données pratiques et carte conservés ; 38 tests statiques cumulés |
| P10 — zones | PASS | liste, détail, carte, facettes et liens ParkItem conservés ; 38 tests statiques cumulés |
| P11 — cartes et plans | PASS | Leaflet, filtres, marqueurs, années et documents source protégés ; 42 tests cumulés |
| P12 — médias | PASS | galeries, lightbox, crédits, lazy loading, vidéos et load more protégés ; 46 tests cumulés |
| P13 — histoire | PASS | timelines, articles, snapshots, comparaison et lignées protégés ; 51 tests cumulés |
| P14 — données pratiques | PASS | horaires, météo, prix, proximité et live protégés ; 56 tests cumulés |
| P15 — notes, classements et actions sociales | PASS | classements, commentaires, collections, watch et partage protégés ; 61 tests cumulés |
| P16 — autres parcours publics | À FAIRE | toutes les familles de routes et données réelles |
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

## Couverture P03 — shells et navigation

- Le header et le footer utilisent toujours la structure complète de `.app-brand-wordmark` ; seul le pictogramme autonome, non sanctuarisé, a été retiré de ces deux compositions.
- Les clés `footer.brand.sloganData` et `footer.brand.sloganFun` restent les seules sources du slogan traduit.
- Les accès Accueil, Parcs, Technique, Classements, Constructeurs, À propos et Admin conditionnel restent présents, comme les contrôles profil/login, unités, langue et thème.
- La navigation mobile conserve ses six accès publics et l’accès Admin conditionnel, avec cible tactile, état actif et `safe-area-inset-bottom`.
- L’arbre dynamique et le trail conservent leurs routes, `queryParams`, niveaux et libellés issus de la facade ; seuls leurs styles ont changé.
- Les shells public, compte et admin conservent leurs outlets et leur composition fonctionnelle. Le public adopte la largeur éditoriale de 1160 px et l’admin garde la largeur dense de 1480 px.
- Preuve ciblée : 17 tests Node réussis, dont contraste du footer et des états actifs, absence de glassmorphism, marque protégée et exhaustivité statique des accès.

## Couverture P04 — accueil réel

- Le hero conserve ses deux CTA, les trois statistiques issues de `homeStats()` et les quatre emplacements issus de `heroFeaturedParks()` ; aucune valeur de la maquette n’est codée en dur.
- Le premier média réel disponible est le seul marqué `eager`/priorité haute ; les trois autres restent lazy. Le composant image, ses `srcset`, tailles, placeholder et texte alternatif existants sont réutilisés.
- La recherche conserve la saisie, le clavier, le bouton explicite, les catégories, l’effacement, l’autocomplétion, les suggestions, les liens par type, les états et la pagination 5/10/20.
- Les vedettes, derniers parcs, derniers articles et le partage sont tous conservés. Le bloc des derniers contenus garde `@defer`, l’hydratation au viewport et le prefetch idle.
- La mise en page transpose la maquette V6 en grille éditoriale, avec quatre cartes dynamiques et un repli sans animation décorative à 960/680/350 px.
- Preuve ciblée cumulée : 24 tests Node réussis. Les tests Angular existants restent la preuve exécutable des handlers et facades et seront lancés avec la suite P21.

## Couverture P05 — catalogue des parcs

- Le terme, la région, le statut, la classification d’audience et la portée de découverte restent connectés aux mêmes inputs/outputs et à la même facade.
- La carte reste une vraie carte Leaflet synchronisée : bounds, marqueurs, sélection, filtres régionaux et événement `markerClick` sont inchangés.
- Les cartes ouvrent toujours les routes de détail ; les résultats mixtes gardent leurs liens typés. Aucun clic n’est transformé en popup.
- La pagination serveur conserve ses liens `pageHref`, son trail, les tailles 6/9/18/36, le retour navigateur et les métadonnées SEO existantes.
- Les tests Angular existants couvrent directement page 2, précédent/suivant, changements de filtre/langue, tailles personnalisées, historique et erreurs HTTP. Ils ne sont pas réécrits par la refonte.
- Preuve ciblée cumulée : 28 tests Node réussis, surfaces catalogue sans gradient ni backdrop et contrats de carte/filtres/pagination présents.

## Couverture P06 — détail de parc

- Le hero conserve image LCP, logo, badges, titre, note, commentaires, collections, watch, visite passeport et tous les liens conditionnels (inventaire, zones, médias, histoire, carte, site officiel).
- Le cycle de vie, le live, les statistiques, la photo secondaire lazy, la description riche, le résumé de contenu, les données pratiques et références sont intacts.
- La colonne pratique conserve géographie, tarification, horaires et météo ; le partage et les parcs proches restent après le contenu principal.
- Les absences de logo, photo, description, géolocalisation ou données pratiques gardent leurs conditions et placeholders existants.
- Une seule image hero reste eager/prioritaire ; les médias secondaires restent lazy et les tailles responsive existantes sont inchangées.
- Preuve ciblée cumulée : 32 tests Node réussis et test de composant existant conservé pour le lien galerie, son nom accessible et ses tailles d’image.

## Couverture P07 — inventaire des ParkItems

- Les filtres texte, catégorie, type, zone et éléments fermés conservent leurs inputs, outputs, compteurs et conditions d’affichage.
- La carte de zone reste une vraie surface Leaflet, avec sélection des marqueurs et état explicite des éléments sans coordonnées.
- Chaque carte conserve sa navigation vers une page ParkItem complète ; aucune ouverture en popup ni modification de route n’est introduite.
- La pagination serveur conserve les tailles 6/12/24/48, le total, les liens et les actions précédent/suivant.
- Les badges, images de repli, états vide/chargement/erreur et le partage restent présents, avec une composition éditoriale plate Playground.

## Couverture P08 — fiches ParkItem

- Les liens précédent/suivant, retours parc/zone et facettes catégorie/type/sous-type conservent les mêmes routes et paramètres.
- Le hero, la description riche, les spécifications par type, l’accès, la géolocalisation conditionnelle, les médias, vidéos, historiques et contenus liés restent intacts.
- Les notes, commentaires, collections, watch, passeport, état live et partage gardent leurs conditions d’authentification et leurs événements.
- La carte n’est rendue que lorsque les coordonnées existent ; aucune géolocalisation ni relation parente n’est inventée.
- Preuve ciblée cumulée P07/P08 : 35 tests Node réussis. Les cas Le Fleury/Rhaegal/L’épée d’Odin/La Catapulte, sans média et non-attraction restent dus à la recette de données P20/P21.

## Couverture P09 — attractions autonomes

- La route autonome conserve son identité propre : aucun parent parc ou ParkItem artificiel n’a été introduit.
- Description riche, caractéristiques, constructeur, état, conditions, image LCP, prix, horaires, météo, localisation, carte conditionnelle, historique et site officiel restent présents.
- Les données tarifaires et liens d’achat proviennent toujours des modèles/API existants ; aucune donnée Pendolino ou valeur de maquette n’est intégrée.
- Preuve ciblée : contrats statiques et surfaces plates inclus dans les 38 tests Node cumulés. Le contrôle d’une attraction publique visible et de Pendolino masqué reste dû à P20/P21.

## Couverture P10 — zones

- La liste conserve quantités réelles, descriptions localisées, highlights par type, lien de détail, retour au parc, état vide et partage.
- Le détail conserve description, compteurs, lien filtré `?zone=`, carte Leaflet avec bounds, cartes ParkItem, état sans marqueur et partage.
- Les parcs avec et sans zone restent distingués par les conditions et états existants ; aucune zone fictive n’est créée.
- Les surfaces adoptent les filets, petits rayons et accents Playground sans modifier les modèles ni la facade.

## Couverture P11 — cartes interactives et plans officiels

- Les modes carte interactive/plans officiels, tabs, filtre fermé, filtres zone/catégorie et états sans géolocalisation restent câblés aux mêmes inputs/outputs.
- Leaflet conserve `fitBounds`, zoom maximal, marqueurs de navigation, sélection catalogue et événement `markerClick`.
- Les plans conservent année, document original, aperçu, langue, taille, nom de fichier, provenance et liens externes.
- Les conteneurs sont plats, réservent toujours leur hauteur et n’ajoutent aucun marqueur ou placeholder présenté comme une donnée réelle.

## Couverture P12 — médias

- Le carrousel conserve filtres année/tag, photo active eager, miniatures lazy, alt, métadonnées, crédits, sources, carte, clavier et lightbox modale.
- Les galeries parc et ParkItem gardent leurs tabs, lots API, `loadMore`, états vide/chargement/erreur et liens de retour.
- Les listes vidéo conservent tabs, type, tag, créateur, détail, miniatures, métadonnées, tags et chargement incrémental.
- Le lecteur conserve embed/externe, précédent/suivant, créateur, fournisseur, tags, erreurs et partage ; aucune vidéo de démonstration n’est ajoutée.

## Couverture P13 — histoire

- Les routes profondes, resolvers, pagination, dates/précision, sujets, événements, images, sources et articles restent inchangés.
- Les snapshots gardent date complète et couverture ; le comparateur garde `fromYear`/`toYear`, ouverts, fermés, modifiés, incertains et stables.
- Les lignées gardent relations, cycles/troncatures, preuves, auteurs/éditeurs et sources externes.
- Les contenus rédigés ne sont ni tronqués ni remplacés ; la refonte se limite aux surfaces plates et à la géométrie.

## Couverture P14 — horaires, météo, prix, proximité et live

- Le calendrier conserve navigation mois, jour sélectionné, horaires, dernière admission, événements, fuseau, couverture et source officielle.
- La météo conserve séries min/max, probabilité et somme de précipitations, vent, graphe SVG, états indisponibles et comparaison historique différée.
- Les prix conservent entrées, crédits, abonnements, parking, web/guichet, validité, conditions, achats, historique, devise, source et fraîcheur.
- La proximité conserve distance/provenance. Le live conserve heure locale, refresh, offline, erreurs, files multiples, filtres, prévision, historique, fraîcheur, méthodologie et attributions.

## Couverture P15 — notes, classements, collections, watch et partage

- Les classements conservent catégories, filtres d’attractions, recherche, chargement incrémental, arbre, liste et routes de méthodologie versionnée.
- La méthodologie conserve seuils de preuve, score bayésien, composition du score parc, règles de publication, historique des versions et canal de signalement.
- Les notes restent éditables par demi-étoile selon les mêmes permissions ; les commentaires gardent rich text localisé, langues, rôles, édition, suppression, images gérées et états.
- Les collections gardent filtres, routes typées, suppression et états. Watch, alertes live et notifications gardent abonnements, pause, seuils, filtres, sources, pagination et signalement.
- Le partage conserve Web Share, copie, réseaux, QR avec ses états et signalement de modération. Les surfaces deviennent plates, à petits rayons et accents par filet.
- Preuve ciblée cumulée : 61 tests Node réussis ; aucune facade, route, API, permission ou règle de classement n’est modifiée.

## Invariants fonctionnels

- Aucun changement d’API, DTO, facade, port, guard, resolver, route, slug, query parameter ou règle de visibilité.
- Aucun changement d’authentification, rôle, pagination serveur, classement, note, collection, watch, passeport, Park Fit, trip ou workflow admin.
- Aucun remplacement de Leaflet, Quill, SSR, hydratation, `TransferState`, `@defer`, lazy loading, cache, SEO, canonical, hreflang ou JSON-LD.
- Aucun nombre, parc, ParkItem, prix, horaire, météo ou événement de la maquette n’est codé en dur.
- Les pages ParkItem restent des pages complètes ; les attractions standalone restent autonomes.
- Les écrans admin gardent leur densité et leurs opérations, sans entrer dans le bundle public initial.

