# Étape 8 — Audit final et préparation publication

Objectif : vérifier que l’intégration complète est cohérente, fiable, localisée et publiable.

## Export requis

Utiliser l’export actualisé après toutes les étapes appliquées.

## Audit de pertinence

Vérifier :

- le parc est bien dans le périmètre `amusement-parks.fun` ;
- les parkItems sont réellement rattachés au parc ou à son histoire ;
- aucune entité douteuse n’a été enrichie artificiellement ;
- les éléments historiques fermés restent visibles quand ils sont utiles.

## Audit JSON

Vérifier :

- `documentType`, `schemaVersion` et `mode` cohérents ;
- aucune section massive inutile ;
- aucune suppression implicite ;
- aucune donnée existante fiable écrasée ;
- toutes les clés sont résolues ;
- toutes les dates complètes sont sourcées ;
- les notes expliquent les incertitudes.
- aucun champ obligatoire n’est cassé ;
- aucun tarif n’est ajouté si les tarifs ne sont pas implémentés ;
- aucun doublon constructeur, exploitant ou fondateur n’est créé ;
- les données existantes fiables sont préservées en mode `merge`.

## Audit contenu public

Vérifier :

- descriptions naturelles et spécifiques ;
- 8 langues présentes sur les lots complets ;
- pas de restrictions, tarifs, horaires ou notes admin dans les descriptions ;
- pas de formulations interdites ;
- titres et résumés historiques lisibles ;
- articles utiles et non redondants.
- les textes ne contiennent pas “upsert”, “SEO”, “contenu public” ou autre jargon interne.
- les restrictions, tailles, horaires, dates, tarifs et coordonnées sont absents des descriptions narratives.

## Audit images

Vérifier :

- URLs externes techniquement importables par le flux remote image ;
- propriétaires résolus ;
- alt texts et crédits localisés ;
- pas de page HTML, preview non téléchargeable, image trompeuse ou watermark non autorisé ;
- images historiques correctement contextualisées.

## Audit horaires et événements

Vérifier :

- horaires sourcés et récents ;
- pas de tarifs ;
- événements nommés seulement ;
- pas de “ouverture estivale” générique transformée en événement ;
- labels et raisons localisés ;
- fermetures exceptionnelles distinctes des fermetures définitives.

## Audit histoire

Vérifier :

- timeline du parc cohérente ;
- timeline des parkItems majeurs cohérente ;
- relocalisations rattachées au bon propriétaire ;
- articles seulement quand il existe un vrai angle ;
- sources présentes sur les événements importants.

## Décision publication

Garder `adminReviewStatus: "ToReview"` tant qu’une relecture humaine reste nécessaire.

Ne passer `isVisible` à `true` que pour les entités :

- pertinentes ;
- sourcées ;
- correctement localisées ;
- sans warning bloquant ;
- prêtes pour le public.

## Sortie attendue

Produire :

- une liste de corrections restantes ;
- ou un dernier JSON upsert ciblé ;
- ou une décision “prêt pour publication” avec risques résiduels.

Ne pas ouvrir un nouveau chantier de fond à cette étape. Les améliorations non bloquantes deviennent des lots séparés.
