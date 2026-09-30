# Visite orpheline

## Déclenchement

Ouvrir l'incident dès qu'une visite ou un passage ne résout plus son propriétaire,
son parc ou sa cible historique alors que l'accès est autorisé.

## Triage

Confirmer avec le propriétaire connecté, relever le trace ID et distinguer une
cible historique fermée d'une véritable rupture de référence.

## Protection immédiate

Mettre les mutations concernées en pause. Ne supprimer, ne recréer et ne rattacher
manuellement aucune visite, aucun passage et aucune note privée.

## Diagnostic

Comparer les références canoniques, les snapshots historiques et l'ownership lu
par l'Application. Vérifier qu'aucun identifiant technique n'est affiché comme
solution de repli.

## Rétablissement

Utiliser une correction idempotente et auditée fondée sur la source de vérité. Si
la cible n'existe plus, conserver son libellé historique plutôt que la remplacer.

## Vérification

Le propriétaire doit relire la visite, son année et tous ses passages. Un autre
compte doit toujours obtenir un refus sans fuite d'existence.

## Escalade

Escalader à `product-passport` lorsque l'ownership ou la cible canonique reste
ambiguë ; ne jamais choisir une cible probable.

## Preuves à conserver

Conserver trace ID, types de référence, statut et heure UTC. Masquer commentaires,
notes, e-mail et identifiants utilisateur.
