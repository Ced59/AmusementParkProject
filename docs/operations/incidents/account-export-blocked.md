# Export de compte bloqué

## Déclenchement

Ouvrir l'incident lorsqu'un export dépasse la durée de son lease, reste en
traitement après réconciliation ou épuise ses reprises bornées.

## Triage

Confirmer le statut, la version, l'âge, le lease et le dernier code d'erreur. Ne
jamais télécharger l'archive d'un membre pour diagnostiquer l'ordonnanceur.

## Protection immédiate

Bloquer un second job concurrent pour le même export et conserver la demande. Ne
pas marquer manuellement l'export prêt.

## Diagnostic

Vérifier lease expiré, stockage, budget de sources, réconciliation et expiration.
Distinguer génération échouée et lien de téléchargement expiré.

## Rétablissement

Laisser le reconciler reprendre idempotemment ou créer une nouvelle tentative
auditée sur le même export selon son état. Ne jamais assembler un export à la main.

## Vérification

Le propriétaire doit recevoir une archive complète, lisible, limitée à ses
données, sans identifiants techniques exposés, puis constater son expiration.

## Escalade

Escalader à `privacy-operations` si une source manque, si un autre compte apparaît
ou si l'archive partielle a été proposée comme complète.

## Preuves à conserver

Conserver statut, version, lease, codes d'erreur, taille et heures UTC. Exclure le
contenu de l'archive et l'identité du membre.
