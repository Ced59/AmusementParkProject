# Inventaire FOUNDATION-01 — identifiants

Date de référence : 31 août 2026. Base : `origin/master` au commit `e1c5f076`.

## Décision

Les identifiants restent des chaînes opaques dans MongoDB, les DTO JSON et les routes. Les nouveaux agrégats utilisent progressivement des value objects typés dans le Core, sans migration globale des entités existantes. La comparaison reste sensible à la casse et aucune normalisation autre que `Trim()` n'est appliquée.

`IdentifierRules.MaximumLength` vaut 256 caractères. Cette limite couvre les formats générés ou codés dans le dépôt tout en laissant une marge aux références historiques et externes. Toute valeur observée au-delà de cette limite doit être inventoriée avant activation d'un mapper concerné ; elle ne doit jamais être tronquée ou remplacée.

## Inventaire statique

| Frontière | Représentation constatée | Compatibilité retenue |
|---|---|---|
| Core historique | `EntityBase.Id` et références en `string` | Inchangé |
| Application | commandes, résultats et ports en `string` | Inchangé aux contrats existants |
| WebAPI | segments de route et DTO en chaîne JSON | Inchangé |
| Infrastructure Mongo | `_id`, `id` et références documentaires en `string` | Inchangé ; aucun serializer global ajouté |
| Angular | modèles et paramètres de route en `string` | Inchangé ; format interne non interprété |
| Nouveaux agrégats | aucun document de visite ou ride avant cette tranche | `VisitId` et `RideOccurrenceId` dans le Core, conversion explicite par `.Value` aux futures frontières |

Les générateurs actuels emploient principalement des UUID sérialisés avec le format `N` (32 caractères) ou le format standard avec tirets (36 caractères). Des constantes fonctionnelles et des références externes non UUID existent également ; le parseur accepte donc tout format opaque valide dans la limite définie.

## Validation et anomalies

- `null`, vide ou whitespace : rejet avec `identifier.required` ;
- plus de 256 caractères : rejet avec `identifier.too-long` ;
- caractère de contrôle : rejet avec `identifier.control-character` ;
- casse : conservée sans conversion ;
- identifiant historique non UUID : accepté ;
- identifiant généré par les nouveaux types : UUID aléatoire au format `N` ;
- identifiant invalide : jamais remplacé silencieusement.

La vérification des valeurs réellement persistées en production reste une opération de diagnostic distincte, sans correction automatique. Elle précède tout backfill de notes ou activation de snapshots de classement et doit produire uniquement des agrégats ou identifiants minimisés.

## Migration et rollback

Aucune collection, aucun index, aucun document et aucun contrat public ne changent dans `FOUNDATION-01`. Le rollback consiste à retirer les nouveaux types avant leur première utilisation persistée. Aucun backfill d'identifiants n'est planifié.
