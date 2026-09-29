# LIVE-11 — Alertes live temporaires

## Finalité métier

LIVE-11 permet à un membre connecté de demander, depuis la fiche d’une
attraction couverte par le direct, une alerte temporaire sur un changement
utile : réouverture, attente sous un seuil, attente au-dessus d’un seuil ou
fonctionnement dégradé. L’alerte reste volontaire, bornée à 1, 3 ou 6 heures ou
à la fin de la journée locale du parc. Elle peut être arrêtée à tout moment.

Les alertes reçues sont regroupées dans le centre WATCH avec l’attraction, le
parc, l’image, l’heure, la source et l’âge exact de l’observation au moment du
déclenchement. Ce premier canal est interne au site : il n’envoie ni courriel ni
notification système et ne promet pas une livraison à la minute près.

## Règles de confiance

Une alerte ne peut être créée que depuis une observation actuellement publique
et fraîche. Le moteur n’examine ensuite que la source publique configurée, un
mapping encore autorisé par les contrôles opérationnels et une observation plus
récente que la précédente. Une donnée vieillissante ou expirée ne peut jamais
déclencher une alerte.

```text
observation fraîche et publique
             │
             ▼
     transition recherchée ? ── non ──► mémoriser le nouvel état
             │ oui
             ▼
 cooldown de 30 min terminé ? ── non ──► mémoriser sans notifier
             │ oui
             ▼
état désarmé + déclenchement durable à livrer
             │
             ▼
notification WATCH idempotente + acquittement du déclenchement
             │
             ▼
 réarmement après retour stable au-delà d’une marge de 5 min
```

La marge de cinq minutes évite qu’un seuil de 30 minutes produise plusieurs
alertes lorsque la source oscille entre 29 et 31 minutes. Après un passage sous
30, l’alerte ne se réarme qu’à 35 minutes ou plus ; la règle inverse s’applique
aux alertes au-dessus d’un seuil. Réouverture et dégradation partagent le même
cooldown de 30 minutes.

## Architecture

- `Core` porte l’abonnement, son expiration, son armement, le cooldown,
  l’hystérésis et la notification. Les décisions sont pures et testées sans
  MongoDB ni fournisseur.
- `Application` vérifie l’utilisateur, la cible publique, la source et la
  fraîcheur, calcule la fin de journée dans le fuseau du parc, orchestre
  l’évaluation et enrichit le résultat avec les noms et images publics.
- `Infrastructure` conserve abonnements et notifications dans deux collections
  dédiées. Le déclenchement à livrer est écrit dans l’abonnement avant la
  notification : une interruption ne peut donc pas perdre l’alerte. Les index
  d’unicité, de quota atomique, d’évaluation, de boîte de réception et TTL sont
  créés automatiquement au démarrage. La suppression de compte purge aussi ces
  données.
- `WebAPI` expose seulement des endpoints `me` authentifiés, sans cache, avec
  contrôle de concurrence par version pour les suppressions et changements
  d’état.
- le frontend accède aux données par un port et des façades dédiés. La création
  est intégrée uniquement à la fiche attraction ; la boîte de réception rejoint
  le centre WATCH existant. Les cartes et actions passent en une colonne sur
  écran étroit et restent bornées au viewport.

## Flux de déclenchement

```text
poll fournisseur
      │
      ▼
normalisation + contrôles LIVE-02…10
      │
      ▼
écriture latest monotone + relecture de la valeur réellement retenue
      │
      ▼
LiveAlertEvaluationService
      │ charge les abonnements actifs de la cible
      ▼
LiveAlertSubscription.Evaluate
      │ transition fraîche + armée + cooldown
      ▼
déclenchement durable dans l’abonnement
      │
      ▼
notification idempotente ──► acquittement ──► centre WATCH ──► fiche attraction
```

Chaque notification possède une clé de déclenchement unique fondée sur
l’abonnement et l’heure observée. Une même observation ne peut donc pas produire
de doublon. Tant que cette notification n’a pas été créée ou reconnue comme
déjà créée, le déclenchement reste durablement en attente et bloque l’avancement
de l’abonnement. Cette sortie reste livrable jusqu’à sept jours après son
déclenchement, même si la durée fonctionnelle de l’abonnement se termine entre-
temps. Les notifications visibles sont conservées 30 jours ; les documents
expirés sont supprimés par TTL. L’expiration métier des abonnements reste
filtrée dans chaque lecture ; leur expiration technique attend seulement la
livraison d’une éventuelle sortie durable, sans dépendre du délai de nettoyage
de MongoDB.

## Modèle MongoDB

```text
live-alert-subscriptions             live-alert-notifications
├─ _id                               ├─ _id
├─ userId                            ├─ userId
├─ targetId / parkId                 ├─ subscriptionId
├─ type / thresholdMinutes           ├─ targetId / parkId
├─ createdAt / expiresAt             ├─ transition + seuil
├─ dernier statut / attente / heure  ├─ sourceId / observedAt / âge
├─ isArmed / lastTriggeredAt         ├─ deliveredAt / status
├─ pendingTrigger (sortie durable)   ├─ expiresAt (TTL)
├─ retentionExpiresAt (TTL)          ├─ triggerKey (unique)
├─ quotaSlot (unique par membre)     │
└─ version                           └─ version
```

Aucune migration manuelle n’est requise : ce sont de nouvelles collections et
l’initialiseur idempotent crée leurs index. Le déploiement peut revenir au code
précédent sans altérer les collections métier existantes ; les TTL continuent
alors à nettoyer les données temporaires.

## Limites volontaires

- 50 alertes actives maximum par membre ;
- seuil compris entre 5 et 300 minutes ;
- une alerte identique par membre, attraction, type et seuil ;
- durée maximale de 12 heures, toujours raccourcie à la fin de la journée
  locale du parc ;
- 50 notifications récentes chargées à la fois ;
- aucune donnée expirée, aucune fusion de sources et aucun identifiant technique
  présenté en remplacement d’un libellé public manquant.
