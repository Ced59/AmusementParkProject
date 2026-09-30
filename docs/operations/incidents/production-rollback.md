# Indisponibilité publique et rollback

## Déclenchement

Le workflow `Production observability` alerte seulement lorsque la même cible
échoue deux fois à trente secondes d'intervalle. Un échec de déploiement déclenche
également ce runbook.

## Triage

Identifier cible, statuts, résultat transport, p95, taille, version et mode SSR.
Vérifier le run de déploiement et l'état de la paire canonique avant toute action.

## Protection immédiate

Ne pas lancer plusieurs déploiements ou rollbacks. Conserver la dernière paire
saine et le journal transactionnel ; ne pas toucher à MongoDB pour une panne HTTP.

## Diagnostic

Lire l'artefact `production-alert-report`, les healthchecks API/front, l'edge et le
journal de déploiement. Distinguer panne locale d'un runner et panne confirmée du
service.

## Rétablissement

Reprendre le déploiement transactionnel selon son journal. Un rollback manuel ne
doit être envisagé que si l'état enregistré l'autorise ; ne jamais nettoyer par
nom ou recréer un service partagé hors maintenance explicite.

## Vérification

Exiger le moniteur vert, les versions front/API cohérentes, le HTML complet, l'API
saine et la paire canonique attestée. Contrôler ensuite une route SSR et une route
publique API.

## Escalade

Escalader à `platform-operations` si le journal est invalide, l'edge indisponible,
un conteneur OOM ou l'identité de la paire ambiguë. Ne pas deviner un état sûr.

## Preuves à conserver

Conserver URL de run, artefact de sonde, versions, identités de génération, codes
de sortie, heures UTC et trace IDs ; ne jamais joindre `.env` ou secret.
