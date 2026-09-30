# Classement incohérent

## Déclenchement

Ouvrir l'incident lorsque le diagnostic des notes confirme au moins un snapshot,
un pointeur courant ou un agrégat incohérent lors de deux lectures espacées d'une
minute.

## Triage

Relever le scope canonique, la révision publiée, l'heure UTC et le trace ID. Ne
jamais lancer deux reconstructions concurrentes.

## Protection immédiate

Conserver le dernier snapshot publié valide. Suspendre uniquement la reconstruction
du scope affecté ; ne pas masquer les notes sources et ne pas recalculer à la main.

## Diagnostic

Utiliser le panneau « Pilotage des classements ». Vérifier l'intégrité des agrégats,
le couple pointeur/snapshot, les chunks et la révision attendue. Distinguer données
insuffisantes et corruption réelle.

## Rétablissement

Employer la recomputation idempotente prévue par le module. En cas de conflit,
relire la révision courante au lieu de forcer l'écriture.

## Vérification

Exiger deux diagnostics sans incohérence, puis contrôler le rang public, ses
preuves et son dénominateur sur le scope exact.

## Escalade

Escalader à `product-rankings` si un snapshot invalide est public ou si la source
ne permet pas une reconstruction sans perte.

## Preuves à conserver

Conserver scope, révisions, compteurs d'intégrité, trace IDs et heures UTC. Exclure
les notes et identifiants personnels des tickets.
