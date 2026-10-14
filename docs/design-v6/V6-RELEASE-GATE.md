# V6 — gate de release 6.0.0

## Décision actuelle

**NO-GO — chantier en cours.** Aucun merge, tag ni déploiement n’est autorisé avant que toutes les lignes soient PASS et que le propriétaire donne son feu vert explicite.

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
| P05–P21 | NON EXÉCUTÉS |
| Version `6.0.0` et historique 8 langues | NON PRÉPARÉS |
| Diff limité au frontend visuel, tests et documentation | NON AUDITÉ |
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

## Rollback

En cas d’échec après feu vert et fusion, appliquer la procédure transactionnelle existante de `docs/deploy/transactional-app-deployment.md` vers la génération complète antérieure. Aucun rollback CSS sélectif, aucune publication partielle et aucun mélange permanent V5/V6.

