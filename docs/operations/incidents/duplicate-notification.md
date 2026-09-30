# Notification dupliquée

## Déclenchement

Ouvrir l'incident lorsque le tableau WATCH affiche un taux de doublons supérieur à
0 % sur au moins vingt livraisons pendant deux relevés espacés de quinze minutes.

## Triage

Identifier le canal, le type factuel, la fenêtre et le nombre de destinataires
affectés. Ne pas lire le corps des notifications.

## Protection immédiate

Suspendre la distribution affectée tout en conservant abonnements, événements et
tentatives. Ne pas renvoyer manuellement une notification ambiguë.

## Diagnostic

Contrôler clés d'idempotence, leases, reprises, tentative de livraison et
réconciliation. Distinguer doublon de création et doublon de transport.

## Rétablissement

Reprendre par le job durable existant après correction de la cause. Une reprise
doit réutiliser la même clé et ne jamais créer un nouvel événement métier.

## Vérification

Observer deux fenêtres sans nouveau doublon et confirmer qu'une notification
légitime unique est encore livrée.

## Escalade

Escalader à `product-watch` si l'idempotence n'est plus démontrable ou si le
prestataire a accepté une requête dont la réponse a été perdue.

## Preuves à conserver

Conserver identifiant de tentative interne masqué, clé d'idempotence hachée,
statuts, compteurs et heures UTC ; exclure destinataires et contenu.
