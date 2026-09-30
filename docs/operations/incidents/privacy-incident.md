# Incident de confidentialité

## Déclenchement

Un cas crédible de donnée privée accessible, journalisée, cachée ou exportée hors
de son périmètre suffit ; ne pas attendre une reproduction large.

## Triage

Qualifier surface, type de donnée, comptes potentiellement affectés, période et
cache. Utiliser les trace IDs et métadonnées minimales.

## Protection immédiate

Couper la capacité minimale concernée, suspendre la publication ou invalider le
cache. Préserver les preuves sans étendre l'accès et sans supprimer précipitamment.

## Diagnostic

Rejouer avec des comptes de test distincts. Vérifier ownership Application,
autorisation HTTP, filtres Mongo, SSR et en-têtes `no-store` dans cet ordre.

## Rétablissement

Corriger l'autorité fautive, invalider les caches et déployer par le parcours
transactionnel. Ne pas affaiblir les contrôles pour rétablir la disponibilité.

## Vérification

Le scénario cross-user doit échouer avant toute lecture du propriétaire. Les
parcours légitimes et les tests QUAL-08 doivent rester verts.

## Escalade

Escalader immédiatement à `privacy-operations`. Toute décision de notification
réglementaire nécessite une analyse humaine du périmètre et des obligations.

## Preuves à conserver

Conserver chronologie, versions, routes modèles, trace IDs et résultats de test.
Masquer jetons, identifiants, e-mails et contenu privé.
