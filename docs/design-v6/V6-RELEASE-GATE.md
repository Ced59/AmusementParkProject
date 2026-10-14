# V6 — gate de release 6.0.0

## Décision actuelle

**NO-GO MERGE — branche prête pour la PR et la CI.** Aucun merge, tag ni déploiement n’est autorisé avant les checks Node 24, la revue visuelle Angular réelle et le feu vert explicite du propriétaire.

| Contrôle | État |
|---|---|
| Branche unique issue du dernier `origin/master` | PASS |
| SHA et version de départ consignés | PASS |
| Inventaire route/template/SCSS/interaction généré | PASS |
| Baseline visuelle V5 initiale | PASS |
| P00 complet | PASS |
| P01 | PASS |
| P02 | PASS CIBLÉ — suite a11y complète à P21 |
| P03 | PASS CIBLÉ — 17 tests statiques, suite Angular complète à P21 |
| P04 | PASS CIBLÉ — 24 tests statiques, suite Angular complète à P21 |
| P05 | PASS CIBLÉ — 28 tests statiques, navigation Angular à P21 |
| P06 | PASS CIBLÉ — 32 tests statiques, données multi-parcs à P20/P21 |
| P07 | PASS CIBLÉ — inventaire, filtres, carte et pagination protégés |
| P08 | PASS CIBLÉ — fiche complète et actions protégées, 35 tests statiques cumulés |
| P09 | PASS CIBLÉ — autonomie, données pratiques et visibilité protégées |
| P10 | PASS CIBLÉ — zones, carte et liens filtrés protégés, 38 tests statiques cumulés |
| P11 | PASS CIBLÉ — Leaflet et plans officiels protégés, 42 tests cumulés |
| P12 | PASS CIBLÉ — médias et lecteurs protégés, 46 tests cumulés |
| P13 | PASS CIBLÉ — histoire et routes profondes protégées, 51 tests cumulés |
| P14 | PASS CIBLÉ — données pratiques et live protégés, 56 tests cumulés |
| P15 | PASS CIBLÉ — classements, commentaires, collections, watch et partage protégés, 61 tests cumulés |
| P16 | PASS CIBLÉ — toutes les familles publiques secondaires protégées, 67 tests cumulés |
| P17 | PASS CIBLÉ — auth, profil, passeport et trips protégés, 74 tests cumulés |
| P18 | PASS CIBLÉ — 58 terminaux et workflows admin protégés, 81 tests cumulés |
| P19 | PASS CIBLÉ — états et composants transversaux protégés, 88 tests cumulés |
| P20 | PASS CIBLÉ — 30 cas Chromium, 13 largeurs, deux thèmes, faible hauteur, 113 tests qualité/a11y |
| Revue visuelle Angular réelle P22 | EN ATTENTE — serveur local interdit, validation sur environnement de revue requise |
| P21 local | PASS CIBLÉ — build SSR, budgets, architecture, a11y/i18n, SEO et tests ciblés verts |
| P21 CI Node 24 | EN ATTENTE — couverture Angular intégrale et tests avec services éphémères |
| Version `6.0.0` et historique 8 langues | PASS — commit de contenu `545345dc3a81c099a328e29a510447339b3f9342` |
| Diff limité au frontend visuel, tests et documentation | PASS — 237 fichiers, 0 suppression, aucun API/deploy/workflow |
| PR unique prête pour revue | NON OUVERTE |
| CI intégralement verte | NON EXÉCUTÉE |
| Revue visuelle propriétaire | NON DEMANDÉE |
| Feu vert explicite de fusion | NON REÇU |
| Déploiement transactionnel et postchecks | NON EXÉCUTÉS |

## Informations de référence

- SHA : `7f6a9088cb20e1fa6e450b2e8f4bacb9e2019a22`
- Version de départ : `5.4.101`
- Branche : `feat/ui-v6-playground-redesign`
- Version cible : `6.0.0`
- Date de cadrage : 14 octobre 2026
- `origin/master` rafraîchi avant release : `7f6a9088cb20e1fa6e450b2e8f4bacb9e2019a22` (aucune divergence entrante)
- SHA de contenu 6.0.0 audité : `545345dc3a81c099a328e29a510447339b3f9342`

## Audit de portée P22

- 237 fichiers modifiés : 169 sous `FRONT/` et 68 sous `docs/`.
- 132 feuilles SCSS, 4 templates, 24 tests/outils MJS, 7 fichiers TypeScript dont 4 specs, 30 preuves WebP, 24 captures baseline PNG, 4 inventaires CSV et les documents contractuels.
- Aucun fichier supprimé ; aucun changement sous `API/`, `deploy/` ou `.github/`.
- `app.routes.ts`, `package-lock.json`, endpoints, DTO, guards, facades, ports, services, persistance et infrastructure inchangés.
- Les seuls TypeScript de production modifiés ajoutent le composant image existant à l’accueil, réalignent les métadonnées `sizes` sur le conteneur V6 et déplacent le style du tooltip vers le SCSS partagé.
- Zéro ajout de `!important`, gradient décoratif, `backdrop-filter`, letter-spacing négatif, `overflow-x: hidden`, dépendance ou URL externe.
- Inventaire final régénéré sans écart : 148 routes et 273 composants.

## Décision avant fusion

La branche peut être poussée et la PR unique ouverte. La fusion reste interdite jusqu’à ce que la CI Node 24 soit entièrement verte, que les captures Angular réelles soient revues et que le propriétaire écrive explicitement son feu vert. La procédure de rollback demeure celle de `docs/deploy/transactional-app-deployment.md` et porte sur une génération complète.

## Rollback

En cas d’échec après feu vert et fusion, appliquer la procédure transactionnelle existante de `docs/deploy/transactional-app-deployment.md` vers la génération complète antérieure. Aucun rollback CSS sélectif, aucune publication partielle et aucun mélange permanent V5/V6.

