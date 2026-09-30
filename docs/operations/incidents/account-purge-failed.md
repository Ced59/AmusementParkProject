# Purge de compte en échec

## Déclenchement

Ouvrir l'incident dès qu'un participant obligatoire échoue ou qu'une donnée
cataloguée subsiste sans règle de rétention après la coordination.

## Triage

Relever la version de purge, les participants terminés, en attente et échoués, sans
copier les données personnelles concernées.

## Protection immédiate

Conserver l'état de reprise et bloquer la clôture. Ne jamais annoncer la suppression
terminée ni relancer deux coordinateurs concurrents.

## Diagnostic

Comparer les participants au catalogue de confidentialité, leurs curseurs et leurs
résultats idempotents. Distinguer rétention légale documentée et reliquat anormal.

## Rétablissement

Reprendre uniquement les participants incomplets avec la version attendue. Toute
nouvelle surface persistée doit rejoindre l'autorité commune, pas un second flux.

## Vérification

Tous les participants obligatoires doivent réussir ; l'audit du catalogue et une
relecture ciblée ne doivent trouver aucun reliquat injustifié.

## Escalade

Escalader immédiatement à `privacy-operations` si la propriété des données, la
rétention ou la portée de la suppression est ambiguë.

## Preuves à conserver

Conserver versions, noms de participants, compteurs, statuts et heures UTC. Ne pas
conserver les valeurs supprimées comme preuve.
