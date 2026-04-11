# P00 — Charte de migration front/back

Ce document est le socle à relire avant chaque nouvelle implémentation de phase.

## Objectif

Poser une base saine et stable pour exécuter la nouvelle roadmap de migration de manière chronologique,
contrôlée et iso-fonctionnelle.

## Ce que P00 fige

- les anciens endpoints `/architecture/*` sont supprimés ;
- l'endpoint `/health` devient la source unique de diagnostic transverse pour le suivi de migration ;
- l'ordre global des phases `P00` à `P18` est figé ;
- les futures implémentations doivent respecter le périmètre exact de leur phase ;
- toute évolution doit rester iso-fonctionnelle tant que la roadmap ne prévoit pas explicitement un changement coordonné.

## Principes obligatoires

1. **Iso-fonctionnel d'abord**  
   Aucun refactor ne doit casser une route, un payload, une navigation ou un scénario existant sans que cela fasse partie du périmètre assumé de la phase.

2. **Une responsabilité claire par phase**  
   Chaque phase doit rester lisible, limitée et vérifiable.

3. **Pas de refactor opportuniste**  
   On ne profite pas d'une phase pour "nettoyer au passage" des zones non prévues.

4. **Découpage avant optimisation**  
   On sépare d'abord les responsabilités, puis on améliore la lisibilité, la robustesse ou la performance locale.

5. **Contrats pilotés explicitement**  
   Quand front et back bougent ensemble, le contrat HTTP doit être traité comme une évolution contrôlée et non comme un effet de bord.

## Ordre des phases

- `P00` — Cadre de migration et gel des conventions
- `P01` — Durcissement back — exposition des endpoints Users
- `P02` — Fondation de l'architecture front — structure cible
- `P03` — Fondation de l'architecture front — contrats transverses
- `P04` — Refactor du socle HTTP front — extraction Auth API
- `P05` — Refactor du socle HTTP front — extraction des API domain services
- `P06` — Back — vraie refonte du refresh token
- `P07` — Évolution coordonnée — bascule vers cookie HttpOnly
- `P08` — Front — stratégie d'état Angular 21
- `P09` — Front — refactor shared et core transverses
- `P10` — Front — refonte clean archi de la feature Parks
- `P11` — Front — refonte clean archi de la feature Park Items
- `P12` — Front — refonte clean archi Admin Parks
- `P13` — Front — refonte clean archi Admin Park Items
- `P14` — Front — refonte clean archi Admin Data / imports
- `P15` — Back — découpage final des gros blocs Infrastructure
- `P16` — Sécurité front ciblée
- `P17` — Back — hygiène finale et cohérence transverse
- `P18` — Finition front — dette résiduelle et cohérence finale

## Définition de fin de phase

Une phase n'est considérée terminée que si :

- le build est OK ;
- la navigation principale n'est pas cassée ;
- aucun changement fonctionnel non prévu n'a été introduit ;
- la diff reste cohérente avec l'objectif de la phase ;
- aucune nouvelle dépendance architecturale illégitime n'a été créée.

## Règles de mise en œuvre

- supprimer plutôt qu'empiler les anciens mécanismes de diagnostic ;
- conserver un point d'entrée de diagnostic unique et simple ;
- garder les conventions visibles dans le code et dans un document de cadrage ;
- préparer les phases suivantes sans implémenter leur contenu en avance ;
- traiter chaque phase comme un incrément autonome, testable et relisible.

## Décision prise en P00

Le projet continue à exposer ses diagnostics de migration via `/health`, mais n'expose plus de contrôleurs dédiés par phase.
Le suivi détaillé des conventions et de l'ordre de la roadmap est désormais centralisé entre :

- `API/ARCHITECTURE/P00-MIGRATION-CHARTER.md`
- `GET /health`
