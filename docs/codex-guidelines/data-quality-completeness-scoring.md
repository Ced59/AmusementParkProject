# AmusementPark — Scoring de complétude data des parcs et parkItems

Version : **2026-07-04-r1**  
Projet : **amusement-parks.fun**  
Usage : spécification fonctionnelle pour calculer en production un score de complétude data, prioriser les enrichissements et aider la décision de visibilité SEO.

## Objectif

Ce document définit un scoring de complétude pour :

- les parcs ;
- les zones quand elles existent réellement ;
- les parkItems ;
- les contenus liés : descriptions, images, horaires, histoire, articles, sources, références et données structurées.

Le score sert à :

- identifier les fiches urgentes à enrichir ;
- repérer les pages visibles qui ne devraient plus l'être ;
- prioriser les grands parcs incomplets ;
- suivre la progression de la qualité data dans le temps ;
- alimenter des filtres admin, tableaux de bord et batchs de nettoyage SEO.

Le score ne remplace pas la validation humaine. Il aide à décider, mais une entité peut rester invisible malgré un bon score si elle est douteuse, hors cible, dupliquée ou non vérifiée.

## Principes structurants

### Score de complétude et visibilité sont liés mais distincts

Le score mesure la richesse et la qualité de la donnée. La visibilité publique reste gouvernée par la règle suivante :

```text
Visible = validé + pertinent + fiable + utile + suffisamment renseigné
```

Un score élevé ne doit jamais rendre visible automatiquement une entité :

- `NotRelevant` ;
- doublon probable ;
- hors cible ;
- non sourcée sur ses données structurantes ;
- avec clé non résolue ;
- avec contenu public trompeur ;
- avec parent invisible.

### Calcul normalisé avec critères applicables

Tous les critères ne s'appliquent pas à toutes les entités. Le score doit donc être calculé avec une logique `applicable / non applicable`.

Formule recommandée :

```text
score = round(earnedPoints / applicableMaxPoints * 100)
```

Les points non applicables ne doivent pas pénaliser l'entité.

Exemples :

- un parc sans zones officielles ne perd pas de points pour absence de zones ;
- un restaurant n'est pas pénalisé parce qu'il n'a pas de constructeur ;
- une attraction sans restriction officielle publiée n'est pas pénalisée si l'absence de source est documentée ;
- un parc fermé n'est pas pénalisé pour absence d'horaires actuels, mais il est attendu sur l'histoire et les dates.

### Les zones ne sont jamais bloquantes par défaut

Beaucoup de parcs n'ont pas de zones officielles. Les zones sont donc un **bonus de structuration**, pas un prérequis bloquant.

Règles :

- si le parc a des zones officielles connues, elles peuvent améliorer le score ;
- si le parc n'a pas de zones officielles, la catégorie zones est `notApplicable` ;
- ne jamais inventer de zones pour améliorer le score ;
- ne jamais masquer un parc uniquement parce qu'il n'a pas de zones ;
- les zones deviennent un sujet de dette seulement si le parc les utilise officiellement et qu'elles manquent.

Cette règle reprend l'esprit de l'étape 2 : créer seulement des zones officielles ou clairement établies, et ne pas inventer de structure si le parc n'a pas de zones publiques fiables.

## Sorties recommandées côté production

Pour chaque parc et parkItem, calculer au minimum :

```text
completenessScore: 0..100
dataQualityLevel: Critical | Weak | Partial | Publishable | Good | Excellent
publicationBlockers: string[]
urgentImprovements: string[]
applicableMaxPoints: number
earnedPoints: number
lastScoreComputedAtUtc: datetime
```

Pour les tableaux de bord, conserver aussi les sous-scores :

```text
identityScore
locationScore
inventoryScore
descriptionScore
mediaScore
openingHoursScore
historyScore
referencesScore
seoScore
auditScore
```

Pour les parkItems :

```text
identityScore
classificationScore
parentAndLocationScore
structuredDetailsScore
accessConditionsScore
descriptionScore
mediaScore
historyScore
referencesScore
seoScore
auditScore
```

## Niveaux de qualité

| Score | Niveau | Usage recommandé |
| ---: | --- | --- |
| 0-29 | `Critical` | Donnée très faible, invisible sauf cas admin exceptionnel |
| 30-49 | `Weak` | Pertinent possible mais non publiable |
| 50-69 | `Partial` | Base utile en admin, généralement invisible |
| 70-84 | `Publishable` | Peut être visible si aucun bloqueur |
| 85-94 | `Good` | Bonne fiche publique, enrichissements secondaires restants |
| 95-100 | `Excellent` | Fiche très complète, à maintenir à jour |

Règle de prudence : un score `Publishable` ou plus n'implique pas `isVisible: true` si `adminReviewStatus` n'est pas `Validated`.

## Bloqueurs de publication

Les bloqueurs suivants forcent `isVisible: false` même si le score brut est élevé :

- entité `NotRelevant` ;
- pertinence projet non confirmée ;
- doublon probable non résolu ;
- nom vide, générique ou placeholder ;
- pays inconnu pour un parc vivant ;
- coordonnées absentes ou manifestement fausses pour un parc vivant ;
- type/statut absent ou incohérent ;
- parent invisible pour un parkItem ;
- zoneKey, manufacturerKey, ownerKey ou itemKey non résolue ;
- image publique avec propriétaire non résolu ;
- description publique trompeuse ou générique ;
- sources critiques absentes pour une fiche historique ;
- URL source d'article ou d'événement non joignable ;
- contenu public contenant jargon interne, notes d'audit ou données techniques au mauvais endroit.

## Scoring des parcs

Le score parc recommandé vaut 100 points normalisés. Les catégories non applicables sont retirées du dénominateur.

### 1. Identité, pertinence et administration — 10 points

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| Nom officiel ou nom public fiable | 2 | Toujours |
| Pertinence projet confirmée | 2 | Toujours |
| `adminReviewStatus` cohérent avec l'état réel | 2 | Toujours |
| Absence de doublon probable | 2 | Toujours |
| Données legacy nettoyées ou dette explicitement documentée | 2 | Toujours |

Exemples de dette : ancien nom non relié, doublon de parc, statut admin resté `ToReview` alors que la fiche est déjà complète.

### 2. Localisation et données générales — 12 points

Inspiré de l'étape 1.

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| Pays renseigné | 1 | Toujours |
| Ville ou localité renseignée | 1 | Parc vivant ou historique localisable |
| Adresse ou repère fiable | 1 | Parc vivant ou historique localisable |
| Coordonnées GPS valides | 2 | Parc vivant ou ancien emplacement connu |
| Coordonnées pointant le parc ou l'entrée principale, pas la ville | 1 | Si GPS présent |
| Type de parc renseigné | 2 | Toujours |
| Statut cohérent (`Operating` / `ClosedDefinitively`) | 1 | Toujours |
| Date d'ouverture ou période fiable | 1 | Si documentable |
| Date de fermeture ou période fiable | 1 | Parc fermé |
| Site officiel ou source d'identité fiable | 1 | Toujours |

Si un parc historique n'a plus d'adresse actuelle, l'ancien emplacement documenté remplace l'adresse publique.

### 3. Audience et positionnement — 4 points

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| `audienceClassification` renseigné | 2 | Toujours |
| Classement cohérent avec sources et notoriété | 1 | Toujours |
| Niveau de traitement aligné avec l'importance du parc | 1 | Toujours |

Un parc majeur notoire classé `Local` sans justification doit être signalé comme dette.

### 4. Inventaire parkItems — 16 points

Inspiré de l'étape 3, qui demande de ne pas se limiter aux coasters et d'intégrer tous les contenus visiteurs fiables.

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| ParkItems principaux présents | 3 | Parc avec attractions ou lieux nommables |
| Inventaire diversifié : attractions, restaurants, boutiques, services, hôtels, parkings, spectacles, animaux selon le parc | 3 | Selon offre réelle |
| Nombre d'items cohérent avec l'importance du parc | 3 | Toujours |
| Items visibles cohérents avec leur qualité | 2 | Si items présents |
| Items fermés importants conservés quand utiles à l'histoire | 1 | Si parc historique ou riche |
| Dates/statuts d'items renseignés quand fiables | 1 | Si documentable |
| Constructeurs/modèles renseignés pour les attractions quand fiables | 1 | Attractions mécaniques |
| Conditions d'accès recherchées et renseignées ou absence documentée | 2 | Attractions avec conditions publiées possibles |

Pour un petit parc avec très peu d'items réels, le score doit juger la cohérence avec la taille du parc, pas un volume absolu.

### 5. Zones et structuration — 4 points non bloquants

Cette catégorie est **non applicable** si le parc n'a pas de zones officielles ou clairement établies.

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| Zones officielles présentes quand elles existent | 2 | Seulement si zones officielles connues |
| ParkItems correctement rattachés aux zones | 1 | Si zones présentes |
| Descriptions ou noms localisés de zones utiles | 1 | Si zones publiques importantes |

Ne jamais pénaliser un parc sans zones officielles. Ne jamais inventer de zones pour gagner ces points.

### 6. Descriptions publiques et localisation — 14 points

Inspiré de l'étape 4.

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| Description du parc spécifique, naturelle et utile | 4 | Toujours |
| Description du parc disponible dans les 8 langues publiques | 2 | Parc publiable |
| Descriptions des parkItems majeurs | 3 | Si parkItems majeurs présents |
| Descriptions des zones | 1 | Seulement si zones présentes |
| Descriptions des restaurants, boutiques, services ou hôtels importants | 1 | Si ces items existent |
| HTML simple et propre | 1 | Si descriptions HTML |
| Absence de formulations interdites et jargon interne | 1 | Toujours si texte public |
| Descriptions non clonées et non génériques | 1 | Toujours si texte public |

Les langues publiques attendues sont `fr`, `en`, `de`, `nl`, `it`, `es`, `pl`, `pt`, sauf dette legacy documentée.

### 7. Images, logos et médias — 10 points

Inspiré de l'étape 5.

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| Logo ou image principale fiable | 2 | Parc publiable |
| Images du parc suffisantes et représentatives | 2 | Parc publiable |
| Images des parkItems majeurs | 2 | Si parkItems majeurs présents |
| Propriétaires d'images résolus | 1 | Si images présentes |
| Alt texts et crédits localisés | 1 | Images publiques |
| Images sans watermark non autorisé ni tromperie éditoriale | 1 | Images publiques |
| Médias originaux, vidéos ou galeries reliés quand disponibles | 1 | Bonus applicable si médias existent |

Une absence d'image peut être acceptable pour une fiche interne, mais elle limite fortement la publication qualitative d'un parc majeur.

### 8. Horaires et calendrier — 8 points

Inspiré de l'étape 6.

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| Horaires actuels ou saisonniers renseignés | 2 | Parc vivant avec horaires publics |
| Source officielle ou fiable | 2 | Parc vivant avec horaires publics |
| Fuseau horaire cohérent | 1 | Si horaires présents |
| Exceptions datées utiles | 1 | Si disponibles |
| Libellés publics réservés aux événements nommés ou exceptions | 1 | Si labels/reasons présents |
| Date de vérification récente | 1 | Horaires actuels/futurs |

Pour un parc fermé définitivement, cette catégorie est non applicable. Le poids doit être reporté par normalisation.

### 9. Histoire, timeline et articles enrichis — 14 points

Inspiré de l'étape 7. Cette catégorie devient importante pour les parcs majeurs, historiques ou fermés.

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| Timeline du parc avec événements structurants | 3 | Parc majeur, historique ou suffisamment documenté |
| Événements majeurs sourcés : fondation, ouverture, extensions, changement d'exploitant, fermeture, etc. | 2 | Si documentable |
| Timeline des parkItems majeurs | 2 | Si parkItems majeurs documentés |
| Articles enrichis pour sujets durables | 2 | Parc majeur, historique ou sujet méritant article |
| Articles avec blocs structurés et angle éditorial réel | 1 | Si articles présents |
| Titres/résumés localisés et lisibles | 1 | Si événements/articles visibles |
| Sources d'événements présentes et vérifiées | 2 | Si événements historiques présents |
| Images historiques ou médias reliés quand pertinents | 1 | Si médias historiques disponibles |

Règle importante : tous les parcs n'ont pas besoin d'articles longs. Un parc local vivant peut être très correct sans article enrichi. En revanche, un parc majeur ou historique sans aucune histoire doit être considéré comme incomplet.

### 10. Références : fondateurs, exploitants, constructeurs — 8 points

Inspiré des étapes 1, 3 et 5.

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| Exploitant actuel ou dernier exploitant renseigné quand fiable | 1 | Si documentable |
| Fondateur renseigné quand fiable | 1 | Si documentable |
| Constructeurs liés aux attractions correctement rattachés | 2 | Attractions mécaniques |
| Biographies/descriptions des références importantes | 2 | Références importantes présentes |
| Références sans doublon et clés résolues | 1 | Si références présentes |
| Site officiel, pays, dates ou informations utiles des références | 1 | Si documentable |

Ne pas créer de référence artificielle pour gagner des points. Une absence de fondateur peut être normale pour un petit parc si aucune source fiable n'existe.

### 11. SEO technique et publication — 6 points

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| Title public unique et pertinent | 1 | Page publique |
| Meta description unique et utile | 1 | Page publique |
| Slug/URL propre et canonique | 1 | Page publique |
| Données JSON-LD ou équivalent cohérentes | 1 | Page publique |
| Page dans sitemap uniquement si visible et validée | 1 | Page publique |
| Pas de thin content ni page placeholder | 1 | Page publique |

Si le front génère automatiquement title/meta, le score peut utiliser les champs sources qui permettent cette génération.

### 12. Audit final et robustesse — 10 points

Inspiré de l'étape 8.

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| Toutes les clés de rattachement résolues | 2 | Toujours |
| Aucune donnée fiable écrasée ou incohérente | 1 | Toujours |
| Sources critiques présentes et vérifiées | 2 | Toujours, avec intensité selon parc |
| Pas de dates inventées ou précision abusive | 1 | Si dates présentes |
| Pas de warning Preview connu ou dette bloquante | 1 | Toujours |
| Données techniques dans les champs structurés, pas dans les descriptions | 1 | Toujours |
| Dettes restantes documentées | 1 | Si score < 100 |
| Relecture humaine effectuée pour publication | 1 | Pour `Validated` |

## Scoring des parkItems

Le score parkItem recommandé vaut 100 points normalisés. Les critères non applicables sont retirés du dénominateur selon la catégorie de l'item.

### 1. Identité, parent et administration — 10 points

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| Nom officiel ou nom fiable | 2 | Toujours |
| Nom non générique et non placeholder | 2 | Toujours |
| Parc parent résolu | 2 | Toujours |
| `adminReviewStatus` cohérent | 1 | Toujours |
| `isVisible` cohérent avec le parent et la qualité | 1 | Toujours |
| Absence de doublon dans le parc | 2 | Toujours |

### 2. Catégorie, type et statut — 12 points

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| `category` renseignée | 2 | Toujours |
| `type` renseigné et cohérent | 2 | Toujours |
| Statut opérationnel ou historique cohérent | 2 | Toujours |
| Dates d'ouverture/fermeture renseignées quand fiables | 2 | Si documentable |
| Ancien item conservé si utile à l'histoire | 1 | Si item fermé |
| Pas de statut saisonnier transformé en statut permanent | 1 | Toujours |
| Source ou preuve de présence | 2 | Toujours |

### 3. Localisation, zone et rattachement — 8 points

Les zones restent non bloquantes.

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| Zone officielle renseignée si le parc a des zones et que l'item y est rattachable | 2 | Seulement si zones fiables existent |
| Coordonnées précises de l'item | 2 | Si emplacement public précis utile |
| Rattachement parent/zone sans clé non résolue | 2 | Toujours |
| Ordre, secteur ou regroupement utile dans la visite | 1 | Si disponible |
| Aucun rattachement inventé | 1 | Toujours |

Si le parc n'a pas de zones officielles, les points de zone sont non applicables et ne pénalisent pas l'item.

### 4. Données structurées propres au type — 16 points

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| Constructeur renseigné pour attraction mécanique quand fiable | 2 | Attractions mécaniques |
| Modèle ou système renseigné quand fiable | 2 | Attractions mécaniques |
| Données techniques utiles et sourcées | 2 | Attractions avec données connues |
| Type d'expérience correctement choisi | 2 | Attractions, shows, restaurants, services |
| Données de service utiles : accès, usage, catégorie, rôle | 2 | Services, parkings, toilettes, lockers, information, first aid |
| Données de restauration/boutique utiles | 2 | Restaurants, snacks, shops |
| Données hôtel/hébergement utiles | 2 | Hotels/resorts |
| Source externe ou officielle reliée aux détails structurés | 2 | Si détails structurés présents |

Ne pas pénaliser un restaurant parce qu'il n'a pas de constructeur. Ne pas pénaliser une attraction si le constructeur est inconnu mais aucune source fiable ne le donne, à condition que la dette soit documentée.

### 5. Conditions d'accès — 10 points

Applicable aux attractions ou expériences où des restrictions peuvent exister.

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| Recherche des conditions d'accès effectuée | 2 | Attractions concernées |
| Taille minimum renseignée si publiée | 2 | Si publiée |
| Taille avec accompagnement ou âge renseigné si publié | 2 | Si publié |
| Restrictions grossesse/santé/transfert renseignées si publiées | 2 | Si publiées |
| Conditions structurées avec enums/unités correctes | 2 | Si conditions présentes |

Si aucune condition n'est publiée par une source fiable, ne pas inventer. Documenter l'absence et rendre la catégorie non applicable ou partiellement applicable selon le modèle de données.

### 6. Description publique et localisation — 14 points

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| Description spécifique, naturelle et utile | 4 | Item publiable |
| Description dans les 8 langues publiques | 2 | Item visible |
| Texte adapté au type d'item | 2 | Toujours si description |
| Pas de restrictions, tarifs, horaires ou technique brute dans la description | 2 | Toujours si description |
| Pas de formulations interdites ou jargon interne | 2 | Toujours si description |
| Non duplication avec autres items similaires | 2 | Toujours si description |

Un parkItem visible avec seulement une description générique doit être masqué ou mis en dette urgente.

### 7. Images et médias — 8 points

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| Image représentative de l'item | 2 | Item visible, si photo disponible |
| Propriétaire d'image résolu | 1 | Si image présente |
| Alt texts et crédits localisés | 1 | Image publique |
| Image non trompeuse, non générique, sans watermark non autorisé | 2 | Image publique |
| Vidéo, galerie ou média original relié si disponible | 1 | Bonus applicable |
| Image historique contextualisée si item fermé | 1 | Item historique avec image |

### 8. Histoire et articles de parkItem — 10 points

Applicable surtout aux attractions majeures, anciennes attractions, relocalisations et items emblématiques.

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| Événement d'ouverture ou présence historique sourcée | 2 | Item majeur ou historique |
| Événements de fermeture, rénovation, changement de nom/thème ou relocalisation | 2 | Si documentable |
| Timeline cohérente avec le parc parent | 2 | Item avec histoire |
| Article enrichi quand l'item le mérite | 2 | Item emblématique |
| Sources vérifiées pour événements et articles | 2 | Si histoire présente |

Un petit service ou restaurant n'a pas besoin d'article historique. Une grande attraction emblématique sans histoire reste incomplète.

### 9. Références et relations — 6 points

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| Constructeur/fabricant relié quand fiable | 2 | Attractions mécaniques |
| Référence constructeur enrichie ou dette documentée | 1 | Constructeur lié |
| Relations avec anciens noms, relocalisations ou item remplacé | 1 | Si documentable |
| Liens internes vers parc, zone, constructeur ou article pertinent | 1 | Item public |
| Aucun doublon de référence créé | 1 | Si références présentes |

### 10. SEO et audit public — 6 points

| Critère | Points | Applicabilité |
| --- | ---: | --- |
| URL/slug propre et canonique | 1 | Item public |
| Title/meta exploitables | 1 | Item public |
| Parent visible et validé | 1 | Item public |
| Pas de page placeholder ou item générique visible | 1 | Item public |
| Données structurées cohérentes | 1 | Item public |
| Relecture humaine ou dette documentée | 1 | Item public |

## Bonus non bloquants

Ces bonus peuvent aider à prioriser mais ne doivent pas masquer les scores de base :

| Bonus | Points indicatifs | Commentaire |
| --- | ---: | --- |
| Captations terrain originales | +3 | Photo/vidéo personnelle datée, bien rattachée |
| Article long de grande qualité | +3 | Parc ou attraction emblématique |
| Sources historiques rares | +2 | Archives, presse ancienne, documents officiels |
| Traductions particulièrement complètes | +2 | 8 langues homogènes avec textes naturels |
| Images très complètes | +2 | Galerie riche et créditée |

Le score final affiché reste plafonné à 100. Les bonus peuvent alimenter un champ séparé `qualityBonusScore`.

## Priorisation des enrichissements

Pour décider ce qu'il faut traiter d'urgence, combiner le score avec l'importance du parc.

### Pondération d'urgence recommandée

```text
priorityScore = (100 - completenessScore) * importanceMultiplier * visibilityMultiplier
```

Multiplicateurs indicatifs :

| Cas | Multiplicateur |
| --- | ---: |
| Parc international ou majeur | 2.0 |
| Parc national | 1.6 |
| Parc régional | 1.3 |
| Parc local | 1.0 |
| Entité visible | 2.0 |
| Entité invisible mais prioritaire | 1.2 |
| Entité `NotRelevant` | 0 |

Exemples :

- parc visible majeur avec score 55 : urgence très haute ;
- parc invisible local avec score 40 : dette normale ;
- parc majeur invisible avec score 80 : candidat à finaliser ;
- parc `NotRelevant` : aucune priorité d'enrichissement.

## Décisions recommandées à partir du score

### Parc

| Condition | Décision |
| --- | --- |
| Bloqueur de publication | `isVisible: false` |
| Score < 70 | `isVisible: false`, sauf exception manuelle temporaire |
| Score 70-84 sans bloqueur | Visible possible pour parc local/intermédiaire |
| Score 85+ sans bloqueur | Visible recommandé |
| Parc majeur score < 85 | Visible seulement si les manques ne touchent pas l'identité, les descriptions, l'inventaire principal ou les sources |
| Parc historique score < 80 | Invisible sauf fiche patrimoniale déjà utile et sourcée |

### ParkItem

| Condition | Décision |
| --- | --- |
| Parent invisible | `isVisible: false` |
| Nom placeholder ou doublon | `isVisible: false` |
| Score < 65 | Invisible ou `ToProcessLater` |
| Score 65-79 | Visible possible si item secondaire et fiable |
| Score 80+ | Visible recommandé si parent visible |
| Attraction majeure score < 80 | Dette prioritaire |

## Champs de données à exposer pour les exports bulk de scoring

Pour calculer correctement le score en batch, les exports doivent fournir autant que possible :

### Parcs

```text
ParkBasics
ParkAdministration
ParkLocation
ParkAudience
ParkSeo
ParkCounts
ParkDescriptions
ParkZones
ParkImages
ParkOpeningHours
ParkHistorySummary
ParkReferencesSummary
ParkAuditFlags
```

Compteurs utiles :

- total parkItems ;
- parkItems visibles ;
- attractions visibles ;
- restaurants/boutiques/services/hôtels/parkings visibles ;
- zones officielles ;
- zones visibles ;
- descriptions par langue ;
- images parc ;
- images parkItems ;
- logo présent ;
- horaires présents ;
- horaires vérifiés récemment ;
- événements historiques ;
- articles enrichis ;
- sources historiques ;
- fondateur/exploitant/constructeurs renseignés ;
- références avec biographies/descriptions ;
- warnings ou flags d'audit.

### ParkItems

```text
ParkItemBasics
ParkItemAdministration
ParkItemParentPark
ParkItemLocation
ParkItemSeo
ParkItemDescriptions
ParkItemImages
ParkItemAttractionDetails
ParkItemAccessConditions
ParkItemHistorySummary
ParkItemReferencesSummary
ParkItemAuditFlags
```

Compteurs utiles :

- descriptions par langue ;
- images ;
- alt texts/crédits ;
- access conditions ;
- constructeur/modèle ;
- dates d'ouverture/fermeture ;
- événements historiques ;
- article enrichi ;
- source officielle ;
- doublon probable ;
- nom placeholder ;
- parent visible ;
- zone résolue ou non applicable.

## Interprétation des absences

Une absence peut avoir trois sens différents. Le scoring doit les distinguer :

| État | Sens | Impact score |
| --- | --- | --- |
| `missing` | Donnée attendue mais absente | Perte de points |
| `notApplicable` | Donnée non pertinente pour cette entité | Retiré du dénominateur |
| `unknown` | On ne sait pas encore si la donnée est applicable | Dette de vérification, légère pénalité ou flag |

Exemple zones :

```text
zonesExpected = false => notApplicable
zonesExpected = true and zonesTotal = 0 => missing
zonesExpected = unknown and zonesTotal = 0 => flag à vérifier, pas bloqueur immédiat
```

## Règle finale

```text
Le score de complétude doit mesurer toute la richesse data réellement utile,
mais il ne doit jamais forcer l'invention de données ni bloquer un parc
pour une catégorie non applicable comme les zones officielles absentes.
```
