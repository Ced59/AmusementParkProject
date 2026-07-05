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
- les années seules fiables sont renseignées comme années seules, sans `01-01` ni premier jour de mois inventé ;
- `park.audienceClassification` est renseigné avec une valeur canonique, sauf reprise volontaire d’un parc legacy explicitement listé comme correction restante ;
- les notes expliquent les incertitudes.
- aucun champ obligatoire n’est cassé ;
- aucun tarif n’est ajouté si les tarifs ne sont pas implémentés ;
- aucun doublon constructeur, exploitant ou fondateur n’est créé ;
- les données existantes fiables sont préservées en mode `merge`.
- toutes les valeurs enum utilisées existent dans `park-graph-upsert-enums.md` ;
- aucun alias legacy ou nombre enum n’est utilisé.

## Audit contenu public

Vérifier :

- descriptions naturelles et spécifiques ;
- 8 langues présentes sur les lots complets ;
- pas de restrictions, tarifs, horaires ou notes admin dans les descriptions ;
- pas de formulations interdites ;
- titres et résumés historiques lisibles ;
- articles utiles et non redondants.
- les textes ne contiennent pas “upsert”, “SEO”, “contenu public” ou autre jargon interne.
- les événements et articles ne contiennent pas “repère documentaire prudent”, “présence publique confirmée”, justification de méthode, note d’audit ou formulation mécanique équivalente.
- les restrictions, tailles, horaires, dates, tarifs et coordonnées sont absents des descriptions narratives.

### Audit articles historiques

Pour chaque article publié ou prêt à publier, vérifier :

- titre spécifique et lisible sur mobile ;
- sous-titre naturel, sans formule générique ;
- résumé éditorial utile, pas une note de méthode ;
- paragraphes fluides, factuels et non redondants ;
- aucune phrase défensive ou méta du type “l’article n’a pas pour but”, “sans dramatisation”, “image contextuelle faute de mieux”, “source faible”, “repère documentaire prudent” ;
- aucune répétition de la description du parc ou du parkItem ;
- les termes sensibles sont nécessaires, sourcés et formulés sobrement ;
- les légendes décrivent l’image affichée et son rapport au sujet, sans justifier l’absence d’une autre image ;
- les incidents ou accidents retenus sur un parkItem ont un article associé et une photo contextualisée quand une image acceptable est trouvable.

Si un article semble écrit comme une note d’audit, une justification de prudence ou une réponse au reviewer, l’étape 8 doit exiger un JSON de correction ciblé avant publication.

## Audit conditions d’accès

Vérifier :

- chaque attraction a été contrôlée pour les conditions d’accès ;
- les conditions trouvées sont dans `items[].attractionDetails.accessConditions[]` ;
- les types et unités utilisent les enums canoniques ;
- les conditions avec accompagnement sont distinguées des tailles ou âges minimum simples ;
- les conditions absentes sont justifiées par une absence de source, pas par un oubli.

## Audit références

Vérifier :

- chaque constructeur lié à un item important a une biographie ou une limite de source documentée ;
- chaque fondateur lié au parc a une biographie ou une limite de source documentée ;
- chaque exploitant lié au parc a une description, des dates ou informations utiles quand elles sont sourçables ;
- les références existantes validées n’ont pas été écrasées ;
- aucun constructeur, exploitant ou fondateur doublon n’a été créé.

## Audit images

Vérifier :

- URLs externes techniquement importables par le flux remote image ;
- propriétaires résolus ;
- aucun warning Preview du type `Remote image ignored: owner could not be resolved` ;
- aucun `ownerKey` basé sur une URL, un nom de fichier, un dossier de galerie ou une valeur devinée ;
- alt texts et crédits localisés ;
- pas de page HTML, preview non téléchargeable, image trompeuse ou watermark non autorisé ;
- images historiques correctement contextualisées.

### Audit images utilisées dans les articles

Pour chaque article avec image :

- l’image existe déjà dans l’export ou est créée dans le même JSON ;
- l’article utilise `imageId` quand l’image vient de l’export ;
- `imageKey` est réservé aux images créées dans le même JSON avec un `key` stable ;
- la légende est localisée dans les langues du lot ;
- la légende ne contient pas de justification technique, juridique ou documentaire ;
- pour incident/accident, une photo réelle non graphique et juridiquement réutilisable doit être privilégiée si disponible ;
- si l’image est seulement contextuelle, elle doit situer le lieu ou l’attraction sans faire croire qu’elle montre l’événement.

## Audit horaires et événements

Vérifier :

- horaires sourcés et récents ;
- pas de tarifs ;
- événements nommés seulement ;
- pas de “ouverture estivale” générique transformée en événement ;
- labels et raisons localisés ;
- `openingHours.labels` et `openingHours.reasons` réservés aux événements nommés, exceptions datées ou informations temporaires vraiment utiles ;
- aucun commentaire général répété sur tous les jours normaux du calendrier ;
- fermetures exceptionnelles distinctes des fermetures définitives.

## Audit histoire

Vérifier :

- timeline du parc cohérente ;
- timeline des parkItems majeurs cohérente ;
- relocalisations rattachées au bon propriétaire ;
- articles seulement quand il existe un vrai angle ;
- résumés d’événements écrits pour les visiteurs, pas comme des notes d’audit documentaire ;
- sources présentes sur les événements importants ;
- toutes les URLs de sources d’articles et d’événements répondent au moment de l’audit ;
- aucune source ne pointe vers une 404, 410, erreur serveur, soft-404, page d’accueil de remplacement ou URL inventée ;
- les archives utilisées sont consultables et correspondent bien au contenu cité.

### Audit résolution history

Avant publication :

- aucun événement `ParkItem` ne doit dépendre uniquement de `itemKey` ou `parkItemKey` ;
- les événements de parkItem existants doivent contenir `ownerId`, `parkItemId`, `itemId` et `contextParkId` explicites ;
- aucun article ne doit référencer une image par `imageKey` si cette clé n’est pas créée ou enregistrée dans le même JSON ;
- Preview doit retourner 0 erreur et 0 warning bloquant avant Apply ;
- les corrections d’articles après audit restent des reprises ciblées de l’étape 7, pas une nouvelle étape.

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

À la fin de la réponse, ajouter `Pertinence de la prochaine étape` et indiquer qu’aucune étape officielle ne suit l’étape 8. Dire que le parcours 0 à 8 est terminé ou lister les corrections ciblées restantes. Ne pas proposer une nouvelle étape de workflow : si une correction appartient à une étape déjà parcourue, la nommer comme reprise ciblée de cette étape.
