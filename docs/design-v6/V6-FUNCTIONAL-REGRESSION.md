# V6 — non-régression fonctionnelle

## Méthode

La matrice générée recense 148 terminaux de route et 1 869 interactions/champs. Les contrôles statiques et la suite Angular vérifient une partie des contrats ; ils ne démontrent pas que chaque ligne a été exercée dans un navigateur avec tous ses états, données et rôles. La matrice de recette complète reste ouverte. Les suites lourdes ne sont pas poursuivies sur le PC si la marge RAM de 1,5 Gio serait menacée.

## Familles obligatoires

| Famille | Scénarios minimum | État |
|---|---|---|
| Navigation | 8 langues, anonyme/auth/admin, back/forward, deep links, 404 | CONTRATS PASS — parcours complets à P20/P21 |
| Recherche | tous types, accents, 0/1/n résultats, suggestions, 5/10/20, pagination | CONTRATS HOME PASS — exécution complète à P21 |
| Parcs | filtres, carte synchronisée, page 2, Le Fleury + profils distincts | CONTRATS LISTE + FICHE PASS — données réelles à P20/P21 |
| ParkItems | catégories, sans média/géoloc, précédente/suivante, 38 lieux du Fleury | CONTRATS LISTE + FICHE PASS — données réelles à P20/P21 |
| Standalone | visibilité, aucun faux parent, historique, prix/météo | CONTRATS PASS — visibilité/données réelles à P20/P21 |
| Zones | parc avec/sans zones, carte, `?zone=`, liens ParkItem | CONTRATS LISTE + FICHE PASS — données réelles à P20/P21 |
| Cartes/plans | Leaflet, filtres, marqueurs, années, provenance, documents | CONTRATS PASS — interactions réelles à P20/P21 |
| Médias | 0/1/8/9/100+, pagination/load more, lightbox, alt/crédits, erreur CDN | CONTRATS PASS — volumes/erreurs réels à P20/P21 |
| Historique | timeline, article, snapshots, comparaison, deep links | CONTRATS PASS — données/SEO réels à P20/P21 |
| Pratique | horaires, météo, prix, live, données absentes/anciennes | CONTRATS PASS — valeurs réelles à P20/P21 |
| Social | notes, commentaires, classements, collections, watch, partage | À TESTER |
| Park Fit/passeport/trips | validations, résultats, brouillons, permissions | CONTRATS PASS — scénarios authentifiés complets à P21 |
| Auth/compte | login, inscription, reset, session expirée, export/suppression | CONTRATS PASS — scénarios authentifiés complets à P21 |
| Admin | 58 écrans, tables, formulaires, import, édition, jobs, dirty/apply | CONTRATS PASS — mutations réelles avec compte de recette à P21 |
| Transversal | consentement, toasts, dialogs, 403/404/500, Quill, unités | CONTRATS PASS — clavier/SR et erreurs intégrées à P20/P21 |

## Contrats gelés

Les fichiers métier, facades, ports, DTO, guards, resolvers, services API, stores et algorithmes ne font pas partie du diff visuel. Toute modification détectée dans ces catégories bloque P22 sauf autorisation séparée.

## Baseline publique vérifiée

- La production sert la version 5.4.101.
- L’accueil, le catalogue, Le Fleury et L’épée d’Odin répondent avec leurs routes canoniques.
- Le thème clair/sombre persiste pendant la navigation testée.
- Les variantes FR, DE et PL chargent leurs contenus localisés.
- Aucun avertissement ni erreur console n’a été relevé pendant la capture ciblée.

Les parcours authentifiés et admin ne sont pas manipulés sur la production. Leur baseline repose sur les templates, tests et fixtures du dépôt, puis sera exercée dans l’environnement isolé de recette.

## Contrôles P03

Les templates conservent toutes les routes, conditions d’authentification et d’administration, commandes de profil, unité, langue et thème. Les tests de composants existants ont été ajustés uniquement pour l’absence du pictogramme optionnel ; leurs assertions sur le wordmark, le slogan, les langues, la version et les modales restent en place. La suite Angular complète sera exécutée à P21 après installation isolée des dépendances.

## Contrôles P04

Le test `playground-v6-home.test.mjs` protège les trois compteurs API, les deux CTA, les quatre parcs dynamiques, toute la chaîne de recherche et de pagination, les contenus différés et le partage. `HomeStateFacade`, ses ports, le debounce, l’exclusion des doublons et les méthodes de routage ne sont pas modifiés. L’ajout visuel du logo de parc passe par `app-image-display` avec une seule image prioritaire.

## Contrôles P05

`playground-v6-catalog.test.mjs` vérifie les cinq dimensions de filtrage/recherche, les sélections carte/liste, les cartes parc et résultats mixtes, Leaflet et la pagination serveur. Les suites existantes `park-list-page.component.spec.ts` et `park-list-page.navigation.spec.ts` conservent les preuves page 2, URL, SEO, back/forward, filtres, changement de langue, 404/503 et tailles de page ; leur exécution intégrale est prévue à P21.

## Contrôles P06

`playground-v6-details.test.mjs` protège toutes les actions et sections de `park-detail-view`, les conditions de données manquantes et la discipline de chargement des images. Aucune facade, mapper, route, donnée ou appel API n’est modifié. La recette Le Fleury + deux profils réels, dans les deux thèmes, demeure un contrôle visuel/données de P20/P21.

## Contrôles P07–P08

`playground-v6-details.test.mjs` protège désormais l’inventaire ParkItem (filtres, carte de zone, éléments sans coordonnées, cartes, pagination serveur 6/12/24/48 et partage) et la fiche complète (précédent/suivant, facettes, rich text, spécifications, accès, Leaflet conditionnel, contenus liés, médias, historique et actions sociales). Les facades, routes et modèles restent inchangés. La validation avec les 38 lieux visibles du Fleury, les quatre exemples nommés et les variantes sans média/non-attraction est réservée à la recette de données P20/P21.

## Contrôles P09–P10

Le même test protège l’autonomie de la fiche standalone, ses données pratiques et son CTA officiel, puis la liste et le détail de zone avec quantités, descriptions, deep links, paramètres de filtre, Leaflet et cartes ParkItem. Aucun modèle, endpoint, état de visibilité ni relation métier n’est modifié. Les cas public/masqué et parc avec/sans zone seront exercés sur données de recette à P20/P21.

## Contrôles P11–P14

- `playground-v6-maps.test.mjs` protège les deux modes carte, les événements Leaflet et les plans officiels multi-années.
- `playground-v6-media.test.mjs` protège filtres, lightbox, alt/crédits/sources, chargements eager/lazy, galeries et vidéos.
- `playground-v6-history.test.mjs` protège routes/resolvers, chronologies paginées, articles complets, snapshots, comparaison et lignées.
- `playground-v6-practical.test.mjs` protège calendrier, météo, prix, proximité et tous les états/attributions live.

Ces contrôles vérifient les contrats statiques sans substituer de donnée. Les gestes Leaflet, grands volumes médias, SEO SSR historique et valeurs API réelles restent bloquants à P20/P21.

## Contrôles P15

`playground-v6-social.test.mjs` protège les filtres et recherches de classement, la méthodologie versionnée, les demi-notes, les commentaires localisés et modérables, les collections, les abonnements watch, les alertes et notifications paginées, ainsi que le partage natif/copie/réseaux/QR et son signalement. Les tests Angular existants restent la preuve des appels de facades et des permissions ; ils seront exécutés avec la suite P21.

## Contrôles P16

`playground-v6-public-complete.test.mjs` vérifie les routes secondaires publiques, le questionnaire/résultat/comparateur Park Fit, les références métier, les passeports partagés et brouillons anonymes, puis les pages contact, confidentialité, sitemap, technique, invitation et erreur. Les contrats de formulaire, filtres, sources, deep links, consentement, import et partage restent explicitement présents.

## Contrôles P17

`playground-v6-account.test.mjs` protège les 21 routes profil authentifiées, les trois routes de récupération, Google/login/inscription, la validation des mots de passe, l’identité et l’avatar, la suppression confirmée, les profils Park Fit, le partage et les statistiques passeport, l’éditeur de visite, les participants, préférences et exports de voyage. Les tests Angular existants restent la preuve des appels de facades, des erreurs serveur et des sessions expirées ; ils seront exécutés à P21.

## Contrôles P18

`playground-v6-admin.test.mjs` relit la matrice générée et échoue si l’un des 58 terminaux, quatre guards hérités, composants ou templates disparaît. Il protège également les routes CRUD, le cycle preview/apply JSON, le drawer contextuel, les transitions historiques, confirmations live, décisions de mapping, import, formulaires à onglets, field mode et batch médias. Les actions destructives ou publiantes ne sont pas exécutées sur des données réelles localement ; leurs tests Angular et la recette isolée restent requis à P21.

## Contrôles P19

`playground-v6-transversal.test.mjs` protège les huit choix de langue, les deux décisions de consentement, le montage global toast/cookie/page-jump, les rôles et annonces des overlays, le clavier des tooltips, les trois états de page, la réduction de mouvement, les uploads et le rich text sanitisé. Il vérifie aussi que les surfaces ciblées sont plates et que l’ancien pictogramme de sélection de langue n’est plus rendu.

## Contrôles P20

`trip-pages-browser-check.mjs` compile les SCSS réels et mesure 30 rendus dans Chrome headless : 13 largeurs contractuelles en clair/sombre, mobile faible hauteur et tablette paysage faible hauteur. Les contrôles portent sur l’overflow global, les bounds de chaque champ/action/panneau, la bottom-nav, les cibles de notation, les espaces réservés des logos/cartes/photos et la justesse des attributs `sizes`. Le rapport versionné contient zéro violation.

`quality:accessibility-i18n` passe intégralement avec 113 tests : 8 langues, collisions i18n sans régression, audit accessibilité sans nouvelle dette, contrastes, thème initial SSR, focus/ARIA, mouvement réduit et invariants V6. Les 28 constats accessibilité historiques et les 1 915 chaînes volontairement identiques à l’anglais restent signalés par les outils, sans nouvelle occurrence imputable à la refonte.

## Audit de non-régression P22

La dernière comparaison des inventaires P00/P22 conserve 148 routes, 273 composants et tous les templates associés. `app.routes.ts`, les modules lazy, guards, resolvers, services, facades, ports, contrats d’input/output et le lockfile restent inchangés. L'exception sécurité PKCS explicitement autorisée et les deux étapes de recette CI sont documentées dans le gate ; aucun fichier n'est supprimé. Une régénération finale demeure requise après le gel visuel.

Les 16 suites contractuelles Playground V6 relisent les actions, conditions, états, permissions, navigations, pagination, médias, cartes, histoire, compte et administration directement dans les templates/routes existants. La suite qualité initiale passe à 113/113 ; la suite Angular ciblée de la fiche parc passe à 1/1. La suite frontend complète avec couverture et le build SSR ont ensuite réussi dans la [CI du commit 0920100cc](https://github.com/Ced59/AmusementParkProject/actions/runs/38642374215). Cela ne solde pas les scénarios navigateur/données réelles listés ci-dessus.

Le 15 octobre, le test Angular ciblé de l'accueil a été arrêté préventivement pendant la compilation à 2,16 Gio disponibles et en baisse. Sa vérification est transférée à la CI. Les corrections de revue concernent les contrastes des boutons repos/survol, les numéros d'étape des voyages et le logo décoratif de l'accueil masqué aux technologies d'assistance.

Le GET public de l'inventaire Fleury a confirmé 38 lieux visibles le 15 octobre. Le complément CI relit ce total, récupère les liens produits par la vraie liste Angular (`size=48`, `closed=all`) puis rend chaque détail séquentiellement pour vérifier son titre. La [CI du commit e98d7f8fb](https://github.com/Ced59/AmusementParkProject/actions/runs/38697638768) confirme **38/38 détails avec leur titre réel**, dont restaurants, services, boutique et spectacle. Il reste une preuve de rendu initial, pas une validation de chaque interaction ou permission. Le contrôle borne la fixture à 96 lieux et échoue explicitement si le volume la dépasse au lieu d'omettre des lignes.

Dans le même run, le smoke `hydratedHome` réussit ses quatre contrôles : racine Angular hydratée, bouton de cookies nécessaires, bascule de thème et recherche réelle retournant Le Fleury. Aucune exception navigateur n'est enregistrée. Toutes les mutations réseau sont bloquées dans ce navigateur de recette ; les tests de notes, compte, import et publication exigent donc toujours un environnement isolé avec comptes de test.

Les nouvelles règles mémoire du 15 octobre imposent aussi 3,5 Gio physiques et 4 Gio de réserve de commit avant opération intensive, ainsi qu'un verrou partagé entre chats. Les contrôles lourds suivants sont déportés en CI. L'incident temporaire à 1 037 processus / 0,78 Gio de commit a imposé une pause ; aucun processus utilisateur n'a été terminé. Les lectures légères ont repris après retour à 329 processus / 11,07 Gio de commit.

