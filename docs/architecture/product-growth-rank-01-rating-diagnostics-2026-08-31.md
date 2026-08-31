# RANK-01 — Diagnostic des notes et des indexes

Date : 2026-08-31
Roadmap : `docs/roadmaps/product-growth/01-ranking-trust-and-methodology-roadmap.md`

## Décision

La mesure préalable aux seuils de classement est exposée par un cas d’usage applicatif en lecture seule. Le contrat applicatif dépend du port `IRatingDiagnosticsReader`; seule l’Infrastructure connaît MongoDB, ses pipelines et ses définitions d’index. Le contrôleur HTTP ne contient aucun calcul métier.

Le rapport est réservé à un compte administrateur activé et non bloqué via `GET /admin/ratings/diagnostics`. Son exécution est limitée à une requête concurrente, sans file d’attente. Chaque pipeline MongoDB possède une durée maximale de trente secondes et peut utiliser le disque afin de ne pas saturer la mémoire du VPS.

## Mesures produites

- volume total et échantillon plafonné à 25 valeurs numériques distinctes ;
- valeurs non numériques, type de stockage inattendu, hors plage, hors demi-point ou seulement proches d’un demi-point ;
- documents sans utilisateur ou cible ;
- doublons de la clé `(UserId, TargetType, TargetId)` ;
- distribution des cibles selon les bandes `1-2`, `3-9`, `10-29`, `30-99` et `100+` contributeurs uniques ;
- agrégats absents, divergents ou sans notes sources ;
- présence, unicité et définition des huit indexes requis par les lectures actuelles.

Le rapport ne retourne aucun identifiant d’utilisateur ou de cible. Cette minimisation évite d’exposer des données personnelles dans un diagnostic d’aide à la décision. Une éventuelle correction de données fera l’objet d’une procédure distincte, sauvegardée, auditée et idempotente.

## Compatibilité et performances

- aucune écriture ni migration MongoDB ;
- aucun changement des contrats publics de notes ou de classement ;
- aucun parcours des documents dans la mémoire de l’API : les regroupements restent côté MongoDB ;
- endpoint non mis en cache et absent des bundles Angular publics et administrateur ;
- la valeur exacte reste vérifiée sans epsilon ; la marge de `0,000001` sert uniquement à compter les anciennes valeurs presque conformes.

## Validation opérationnelle

Après le premier déploiement, le rapport de production doit être exécuté une fois. Les volumes minimisés et la décision explicite concernant le backfill `ValueHalfSteps` sont consignés dans la PR, sans identifiant ni secret.

## Retour arrière

Supprimer l’endpoint, le handler, le port et son implémentation rétablit l’état précédent. Aucune donnée ni définition d’index n’est modifiée par ce diagnostic.
