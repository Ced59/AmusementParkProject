# V6 — non-régression fonctionnelle

## Méthode

La matrice générée recense 148 terminaux de route et 1 869 interactions/champs. Chaque ligne est vérifiée statiquement pendant l’implémentation puis exercée par les tests existants, les tests ciblés V6 et la CI. Les suites lourdes ne sont pas lancées sur le PC si la marge RAM de 1,5 Gio ne peut pas être tenue.

## Familles obligatoires

| Famille | Scénarios minimum | État |
|---|---|---|
| Navigation | 8 langues, anonyme/auth/admin, back/forward, deep links, 404 | À TESTER |
| Recherche | tous types, accents, 0/1/n résultats, suggestions, 5/10/20, pagination | À TESTER |
| Parcs | filtres, carte synchronisée, page 2, Le Fleury + profils distincts | À TESTER |
| ParkItems | catégories, sans média/géoloc, précédente/suivante, 38 lieux du Fleury | À TESTER |
| Standalone | visibilité, aucun faux parent, historique, prix/météo | À TESTER |
| Médias | 0/1/8/9/100+, pagination/load more, lightbox, alt/crédits, erreur CDN | À TESTER |
| Historique | timeline, article, snapshots, comparaison, deep links | À TESTER |
| Pratique | horaires, météo, prix, live, données absentes/anciennes | À TESTER |
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

