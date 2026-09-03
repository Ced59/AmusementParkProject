# PASS-02 — Agrégat de visite privée

Date : 2026-09-03

Roadmap : `docs/roadmaps/product-growth/02-visit-passport-and-ride-log-roadmap.md`

## Résultat

Le Core contient désormais l'agrégat `Visit`. Il porte les règles d'une session déclarée par une personne dans un parc, sans dépendre de MongoDB, du protocole HTTP ou d'une horloge globale.

L'identité de la visite, l'utilisateur propriétaire, le parc et la confidentialité sont immuables. La première version n'accepte que `VisitPrivacy.Private` ; les valeurs `Unlisted` et `Public` réservent l'évolution du contrat sans rendre les visites partageables prématurément.

## État initial et mutations

Une visite est créée en `Draft`, à la version 1. Chaque mutation effective incrémente la version exactement une fois et reçoit un timestamp UTC explicite. Une mise à jour identique ne fabrique ni nouvelle version ni nouveau timestamp.

```text
                    ┌─────────────┐
                    │  Archived   │
                    └──────┬──────┘
                      ▲     │  restore as Draft/Completed
              archive │     ▼
┌─────────┐ complete ┌┴───────────┐
│  Draft  ├─────────►│ Completed  │
└────▲────┘          └─────┬──────┘
     └──── reopen ─────────┘
```

- seule une visite `Draft` peut être modifiée ou complétée ;
- une correction d'une visite terminée exige une réouverture explicite ;
- `Archived` peut provenir de `Draft` ou `Completed` ;
- une restauration choisit explicitement `Draft` ou `Completed` ;
- le timestamp de complétion est conservé lors de l'archivage d'une visite terminée et supprimé lors d'une restauration en brouillon ;
- `Deleted` n'est pas un état public : suppression et tombstone appartiennent au workflow RGPD ultérieur.

## Temps et dates futures

Le domaine ne consulte jamais `DateTime.UtcNow`. L'Application lui fournira l'instant UTC et le jour local du parc issus de ses abstractions.

Une visite ne peut devenir `Completed` que si sa période déclarée n'est pas entièrement future par rapport au jour local. Pour une date partielle, le contrôle utilise sa borne la plus ancienne sans lui inventer de jour : le mois courant et l'année courante restent donc valides, tandis qu'une année, un mois ou un jour entièrement futurs sont rejetés.

Le fuseau est stocké comme contexte facultatif de la visite. Cette tranche normalise sa forme et borne sa taille ; la vérification IANA et les cas DST seront réalisés par un port applicatif avant les commandes nécessitant une heure locale.

## Données privées et limites

- titre facultatif : 160 caractères, une seule ligne ;
- note privée facultative : 4 000 caractères, retours à la ligne autorisés ;
- identifiant de fuseau : 128 caractères, sans caractère de contrôle ;
- chaînes vides normalisées en absence de valeur ;
- identifiants utilisateur et parc validés par les règles communes d'identifiants opaques.

Ces limites bornent les futurs documents et payloads sans empêcher un récit personnel utile. Aucune donnée de visite n'est encore persistée ou exposée.

## Concurrence

`Version` est une révision positive et monotone. PASS-03 l'utilisera dans le filtre Mongo d'écriture afin qu'une version attendue obsolète échoue au lieu d'écraser silencieusement une modification concurrente. L'idempotence des commandes restera une responsabilité d'orchestration Application/Infrastructure, distincte des invariants de l'agrégat.

## Preuves

Les tests purs couvrent notamment :

- création et normalisation ;
- plusieurs visites pour le même parc et la même date ;
- confidentialité privée obligatoire ;
- transitions autorisées et interdites ;
- réouverture et restauration ;
- dates exactes et partielles futures ;
- timestamps UTC et ordre chronologique ;
- version positive, incrément unique et débordement ;
- limites des textes et conservation des notes multilignes ;
- restauration d'un état persistant cohérent.

## Retour arrière

Cette tranche ne crée ni collection, ni index, ni endpoint, ni route Angular. Son retrait supprime uniquement l'agrégat et ses types d'état ; les notes communautaires et les classements restent inchangés.
