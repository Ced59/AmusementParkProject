# Incidents opérables

Ce dossier est l'autorité versionnée de `QUAL-09`. Le catalogue relie chaque
symptôme à un seuil, une fenêtre, un propriétaire, un repli sûr, un runbook et une
preuve de reprise. Les seuils ne remplacent jamais l'analyse humaine des données
privées ou métier.

Le moniteur public exécute les cibles sans authentification toutes les quinze
minutes. Un échec n'est confirmé qu'après une seconde sonde trente secondes plus
tard. L'échec du workflow GitHub constitue l'alerte ; il n'ajoute ni service sur le
VPS, ni secret, ni collecte utilisateur. Les incidents métier utilisent leurs
panneaux administrateur ou une escalade support, car aucune métrique inexistante
n'est inventée.

Avant toute action : réduire l'impact sans supprimer de données, conserver le
trace ID et les heures UTC, ne jamais copier de contenu privé dans un ticket, puis
suivre le runbook lié depuis [`catalog.json`](catalog.json).

Contrôles reproductibles depuis `FRONT/AmusementPark` :

```bash
npm run operations:readiness:test
npm run operations:readiness
npm run operations:production-monitor
```
