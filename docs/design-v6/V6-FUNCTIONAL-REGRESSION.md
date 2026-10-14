# V6 — non-régression fonctionnelle

## Méthode

La matrice générée recense 148 terminaux de route et 1 869 interactions/champs. Chaque ligne est vérifiée statiquement pendant l’implémentation puis exercée par les tests existants, les tests ciblés V6 et la CI. Les suites lourdes ne sont pas lancées sur le PC si la marge RAM de 1,5 Gio ne peut pas être tenue.

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
| Park Fit/passeport/trips | validations, résultats, brouillons, permissions | À TESTER |
| Auth/compte | login, inscription, reset, session expirée, export/suppression | À TESTER |
| Admin | 58 écrans, tables, formulaires, import, édition, jobs, dirty/apply | À TESTER |
| Transversal | consentement, toasts, dialogs, 403/404/500, Quill, unités | À TESTER |

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

