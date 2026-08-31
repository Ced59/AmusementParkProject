# FOUNDATION-02 — représentation exacte des notes

Date de référence : 31 août 2026. Base : `origin/master` au commit `f10bf90a`.

## Décision

Le Core représente désormais une note valide avec `RatingValue`, soit un entier de 1 à 10 demi-points. Les conversions `decimal` et `double` n'acceptent que les dix valeurs exactes comprises entre 0,5 et 5 ; aucune tolérance flottante n'est utilisée pour transformer une valeur proche en note valide.

Les contrats existants, `UserRating.Value`, `RatingAggregate.RatingSum` et les documents Mongo restent en `double` dans cette tranche. La compatibilité est assurée par conversion explicite des valeurs historiques valides et par des calculateurs fondés sur une somme entière de demi-points.

## Diagnostic

`RatingValue.TryFromDouble` permet de classer une valeur historique sans exception :

- hors plage, non finie ou égale à zéro : `rating.invalid-value` ;
- dans la plage mais hors pas de 0,5 : `rating.invalid-step` ;
- valeur exacte : demi-points disponibles sans arrondi.

Aucune anomalie n'est corrigée automatiquement. L'inventaire de la collection de production doit précéder tout ajout de `ValueHalfSteps`, backfill ou reconstruction d'agrégat. Les rapports ne doivent exposer que des volumes et des identifiants minimisés.

## Compatibilité mathématique

Pour des sources valides :

```text
sumHalfSteps = ratingSum × 2
average = (sumHalfSteps / 2) / ratingCount
bayesian = ((sumHalfSteps / 2) + priorMean × priorWeight) / (ratingCount + priorWeight)
```

Les fixtures comparent directement les nouveaux calculs aux fonctions historiques. L'absence de note reste représentée par `null` dans les futurs modèles ; la valeur `0` n'est jamais une note.

## Migration et rollback

Aucune collection, aucun index et aucun contrat public ne changent dans `FOUNDATION-02`. Le rollback consiste à retirer le value object et les surcharges exactes ; les champs historiques restent intacts. Toute écriture duale éventuelle fera l'objet d'une PR distincte après mesure.
