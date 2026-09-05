# AmusementPark — Enums JSON Park Graph Upsert

Version : **2026-09-05-r1**

Ce fichier liste les valeurs enum à utiliser dans les JSON `AmusementParkParkGraphUpsert` et `standaloneAttractionGraph`.

Règles :

- écrire les valeurs en chaînes canoniques, par exemple `"RollerCoaster"` ;
- ne jamais envoyer de valeur numérique ;
- ne pas localiser les enums ;
- ne pas s’appuyer sur les tolérances internes de casse, espaces, tirets ou underscores ;
- avant de livrer un JSON, vérifier chaque valeur enum utilisée avec ce fichier.

## Champs parc

| Champ JSON | Enum | Valeurs |
| --- | --- | --- |
| `park.type` | `ParkType` | `ThemePark`, `WaterPark`, `Zoo`, `AnimalPark`, `AmusementPark`, `Resort` |
| `park.audienceClassification` | `ParkAudienceClassification` | `International`, `National`, `Regional`, `Local` |
| `park.status` | `ParkStatus` | `Planned`, `UnderConstruction`, `Operating`, `TemporarilyClosed`, `ClosedDefinitively`, `Cancelled` |
| `park.adminReviewStatus` | `AdminReviewStatus` | `ToReview`, `Validated`, `ToProcessLater`, `NotRelevant` |
| `park.officialMaps[].format` | `ParkOfficialMapFormat` | `Image`, `Pdf`, `Other` |

`AdminReviewStatus.Ready` existe comme alias legacy de `Validated`, mais ne doit pas être utilisé dans les nouveaux JSON.

### Cycle de vie d’un parc

Toujours écrire la valeur canonique correspondant à la réalité documentée :

| Valeur | Usage |
| --- | --- |
| `Planned` | Projet officiellement annoncé, sans chantier confirmé. |
| `UnderConstruction` | Chantier du parc effectivement commencé. |
| `Operating` | Parc actuellement exploité et visitable selon son calendrier. |
| `TemporarilyClosed` | Parc existant fermé temporairement, avec une reprise encore possible. |
| `ClosedDefinitively` | Parc ayant existé puis fermé définitivement. |
| `Cancelled` | Projet annoncé puis annulé ou abandonné avant ouverture. |

Les anciennes valeurs numériques stables restent `Operating = 0` et `ClosedDefinitively = 1`. Les JSON upsert et les exports doivent néanmoins utiliser exclusivement les chaînes canoniques.

Le parseur tolère notamment `announced`, `construction`, `temporaryClosure`, `canceled` et leurs variantes normalisées pour reprendre d’anciens brouillons. Cette tolérance n’autorise pas leur emploi dans un nouveau livrable : la Preview et l’export normalisent toujours vers les six valeurs de la table.

Seul `Operating` autorise un bloc `openingHours`. Ne pas créer de faux horaires, dates d’ouverture quotidienne ou propriétés opérationnelles pour un projet, un parc temporairement fermé, un ancien parc ou un projet annulé. Une date d’ouverture/fermeture approximative fiable reste textuelle ; ne jamais inventer un jour ou un mois.

Si un ancien enregistrement non opérationnel possède encore des horaires stockés, l’export borné renvoie `openingHours: null`. Une Preview contenant des règles d’horaires pour un autre statut est bloquée avec une erreur explicite au lieu de réimporter ces données.

## Champs parkItems

| Champ JSON | Type métier | Valeurs |
| --- | --- | --- |
| `items[].category` | `ParkItemCategory` | `Attraction`, `Restaurant`, `Hotel`, `Animal`, `Show`, `Shop`, `Service`, `Transport`, `Other` |
| `items[].type` | `ParkItemType` | `Attraction`, `RollerCoaster`, `WaterRide`, `FlatRide`, `DarkRide`, `FamilyRide`, `ThrillRide`, `TransportRide`, `WalkThrough`, `Playground`, `InteractiveExperience`, `ObservationRide`, `AnimalExhibit`, `Restaurant`, `Snack`, `Hotel`, `Show`, `Shop`, `Game`, `MeetAndGreet`, `Service`, `Toilets`, `FirstAid`, `Information`, `Locker`, `Parking`, `Transport`, `Station`, `Other`, `Cinema`, `DropTower` |
| `items[].adminReviewStatus` | `AdminReviewStatus` | `ToReview`, `Validated`, `ToProcessLater`, `NotRelevant` |
| `items[].attractionDetails.status` | chaîne lifecycle contrôlée | `Operating`, `UnderConstruction`, `TemporarilyClosed`, `ClosedDefinitively`, `Removed`, `Planned`, `Unknown` |
| `items[].attractionDetails.waterExposureLevel` | `AttractionWaterExposureLevel` | `None`, `Splash`, `Moderate`, `Soaking`, `ExtremeSoaking` |
| `items[].attractionDetails.accessConditions[].type` | `AttractionAccessConditionType` | `MinHeight`, `MinHeightAccompanied`, `MaxHeight`, `MinAge`, `MinAgeAccompanied`, `PregnancyRestriction`, `HeartRestriction`, `BackNeckRestriction`, `WheelchairTransferRequired`, `AccessPassRequired`, `Custom` |
| `items[].attractionDetails.accessConditions[].unit` | `AttractionAccessConditionUnit` | `Centimeter`, `Inch`, `Year` |

### `attractionDetails.status` — état courant, jamais événement historique

`items[].attractionDetails.status` est techniquement stocké comme une chaîne pour assurer la compatibilité avec des données legacy. Ce choix d’implémentation ne rend pas le champ sémantiquement libre. Dans un nouveau JSON ou une correction de complétude, utiliser uniquement le vocabulaire lifecycle contrôlé ci-dessus.

| Valeur | Sens actuel |
| --- | --- |
| `Operating` | Attraction actuellement exploitable dans son incarnation et son emplacement courants. |
| `UnderConstruction` | Attraction confirmée dont la construction ou l’installation est en cours et qui n’a pas encore ouvert. |
| `TemporarilyClosed` | Attraction existante momentanément indisponible, avec une réouverture attendue ou encore plausible. |
| `ClosedDefinitively` | Attraction dont l’exploitation dans son incarnation actuelle est terminée définitivement, même si l’installation ou certains éléments existent encore sur place. |
| `Removed` | Attraction qui n’est plus installée ou présente comme attraction exploitable dans ce parc ; ce statut convient notamment après démontage, transfert hors du parc ou démolition. |
| `Planned` | Attraction officiellement annoncée mais pas encore en chantier confirmé. Les libellés `Annoncé`, `Announced` ou équivalents sont des alias de sens et ne doivent pas être stockés à la place de `Planned`. |
| `Unknown` | État courant impossible à établir de manière fiable après recherche ; ne pas l’utiliser comme échappatoire à une recherche incomplète. |

Les mots qui décrivent **ce qui est arrivé** à une attraction ne sont jamais des statuts. Ils appartiennent à `history.events[].eventType`. Sont notamment interdits dans `attractionDetails.status` : `Retracké`, `Retracked`, `Délocalisé`, `Relocated`, `Relocalisé`, `Renommé`, `Rethemé`, `Rebuilt`, `Reconstruit`, `Rénové`, `Refurbished`, `Remplacé`, `Replaced`, `Démoli`, `Demolished`, `Stocké`, `Stored`, `Vendu`, `Sold`, `Transféré`, `Transferred`, `Réinstallé`, `Reinstalled` et toute formulation équivalente.

Le principe est :

- le **statut** répond à « dans quel état cette attraction se trouve-t-elle maintenant dans ce parc ? » ;
- la **timeline** répond à « quels faits durables lui sont arrivés, quand et dans quel contexte ? ».

Exemples obligatoires :

| Situation | `attractionDetails.status` | Événement(s) history |
| --- | --- | --- |
| Retrack terminé et attraction rouverte | `Operating` | `Retrack`, puis `Reopening` si la réouverture mérite son propre jalon |
| Retrack ou rénovation en cours avec fermeture au public | `TemporarilyClosed` | `Retrack`, `Refurbishment` ou `Rehab` selon le fait documenté |
| Attraction renommée mais toujours ouverte | `Operating` | `Rename` |
| Attraction rethemée mais toujours ouverte | `Operating` | `ThemeChange` |
| Attraction déplacée à un autre emplacement du même parc et rouverte | `Operating` | `RelocationDeparture`, `RelocationArrival` et éventuellement `Reinstallation` |
| Attraction partie vers un autre parc | `Removed` dans le parc d’origine | `RelocationDeparture`, `Transfer` ou `Sale`; le parc d’arrivée porte sa propre vie |
| Attraction démontée et stockée hors exploitation | `Removed` | `Dismantling`, puis `Storage` |
| Attraction démolie | `Removed` | `Demolition` |
| Attraction définitivement arrêtée mais encore présente | `ClosedDefinitively` | `DefinitiveClosure` |
| Ancienne attraction remplacée par une autre | `ClosedDefinitively` ou `Removed` selon sa présence réelle | `Replacement`; la nouvelle attraction possède son propre statut |

Si un export legacy contient une transformation historique dans `attractionDetails.status`, ne jamais simplement supprimer cette information. La reprise doit :

1. déterminer le vrai cycle de vie courant et corriger `attractionDetails.status` ;
2. rechercher la date ou période et les sources du fait historique ;
3. créer ou corriger le ou les `history.events` correspondants ;
4. conserver une précision `Year` ou `Month` si la date exacte n’est pas fiable, sans inventer de jour.

## Conditions d’accès

Les conditions d’accès d’une attraction vont dans `items[].attractionDetails.accessConditions[]`.

Champs acceptés dans une condition :

- `type` ;
- `typeKey` ;
- `isCustom` ;
- `customTypeKey` ;
- `customTypeLabel` ;
- `value` ;
- `unit` ;
- `requiresAccompaniment` ;
- `minimumCompanionAge` ;
- `label` ;
- `description` ;
- `displayOrder`.

Utiliser `Centimeter` pour une taille en centimètres, `Inch` pour une taille en pouces et `Year` pour un âge. Le flux normalise les mesures, mais le JSON doit rester clair et sourcé.

## Champs standaloneAttraction

Les attractions fixes isolées utilisent les mêmes valeurs techniques qu’un parkItem attraction, mais dans `standaloneAttraction`.

| Champ JSON | Enum | Valeurs |
| --- | --- | --- |
| `standaloneAttraction.type` | `ParkItemType` | `Attraction`, `RollerCoaster`, `WaterRide`, `FlatRide`, `DarkRide`, `FamilyRide`, `ThrillRide`, `TransportRide`, `WalkThrough`, `Playground`, `InteractiveExperience`, `ObservationRide`, `AnimalExhibit`, `Other`, `Cinema`, `DropTower` |
| `standaloneAttraction.adminReviewStatus` | `AdminReviewStatus` | `ToReview`, `Validated`, `ToProcessLater`, `NotRelevant` |
| `standaloneAttraction.attractionDetails.waterExposureLevel` | `AttractionWaterExposureLevel` | `None`, `Splash`, `Moderate`, `Soaking`, `ExtremeSoaking` |
| `standaloneAttraction.attractionDetails.accessConditions[].type` | `AttractionAccessConditionType` | `MinHeight`, `MinHeightAccompanied`, `MaxHeight`, `MinAge`, `MinAgeAccompanied`, `PregnancyRestriction`, `HeartRestriction`, `BackNeckRestriction`, `WheelchairTransferRequired`, `AccessPassRequired`, `Custom` |
| `standaloneAttraction.attractionDetails.accessConditions[].unit` | `AttractionAccessConditionUnit` | `Centimeter`, `Inch`, `Year` |

Pour `standaloneAttraction.attractionDetails.status`, appliquer le même contrat lifecycle contrôlé que pour `items[].attractionDetails.status`. Les transformations de l’attraction autonome appartiennent à sa timeline, pas à son statut.

Exemples :

```json
{
  "type": "MinHeight",
  "value": 120,
  "unit": "Centimeter",
  "displayOrder": 1
}
```

```json
{
  "type": "MinHeightAccompanied",
  "value": 100,
  "unit": "Centimeter",
  "requiresAccompaniment": true,
  "minimumCompanionAge": 16,
  "displayOrder": 2
}
```

```json
{
  "type": "PregnancyRestriction",
  "label": [
    { "languageCode": "fr", "value": "Déconseillé pendant la grossesse" },
    { "languageCode": "en", "value": "Not recommended during pregnancy" }
  ],
  "displayOrder": 3
}
```

## Champs références

| Champ JSON | Enum | Valeurs |
| --- | --- | --- |
| `references.operators[].adminReviewStatus` | `AdminReviewStatus` | `ToReview`, `Validated`, `ToProcessLater`, `NotRelevant` |
| `references.manufacturers[].adminReviewStatus` | `AdminReviewStatus` | `ToReview`, `Validated`, `ToProcessLater`, `NotRelevant` |

## Champs images

| Champ JSON | Enum | Valeurs à utiliser dans le Park Graph Upsert |
| --- | --- | --- |
| `images[].ownerType` | `ImageOwnerType` | `Park`, `ParkItem`, `ParkOperator`, `AttractionManufacturer`, `ParkFounder`, `StandaloneAttraction` |
| `images[].category` | `ImageCategory` | `Avatar`, `Logo`, `Park`, `ParkItem`, `Operator`, `Manufacturer`, `Founder`, `VideoThumbnail`, `StandaloneAttraction` |

`ImageOwnerType.None`, `User`, `Video` et l’alias legacy `Attraction` existent côté domaine, mais ne doivent pas être utilisés dans ce flux d’intégration de parc ou d’attraction isolée.

`ImageCategory.Attraction` existe comme alias legacy de `ParkItem`, mais les nouveaux JSON doivent utiliser `ParkItem`.

## Horaires

| Champ JSON | Enum | Valeurs |
| --- | --- | --- |
| `openingHours.regularRules[].daysOfWeek[]` | `DayOfWeek` | `Monday`, `Tuesday`, `Wednesday`, `Thursday`, `Friday`, `Saturday`, `Sunday` |

Les dates d’horaires utilisent `yyyy-MM-dd`. Les heures utilisent `HH:mm`.

## Tarifs

| Champ JSON | Enum | Valeurs |
| --- | --- | --- |
| `pricing.admissionOffers[].onlinePrice.mode` / `gatePrice.mode` | `ParkPricingMode` | `Fixed`, `Range`, `Dynamic` |
| `pricing.annualPasses[].onlinePrice.mode` / `gatePrice.mode` | `ParkPricingMode` | `Fixed`, `Range`, `Dynamic` |
| `pricing.parkingOffers[].onlinePrice.mode` / `gatePrice.mode` | `ParkPricingMode` | `Fixed`, `Range`, `Dynamic` |

`Fixed` exige `amount`. `Range` exige `minimumAmount` et `maximumAmount`. `Dynamic` accepte des bornes facultatives. Dans tous les cas, les montants restent positifs ou nuls et une borne minimale ne dépasse jamais la borne maximale.

## Histoire

| Champ JSON | Enum | Valeurs |
| --- | --- | --- |
| `history.events[].entityType` | `HistoryEntityType` | `Park`, `ParkItem` |
| `history.events[].datePrecision` ou `precision` | `HistoryDatePrecision` | `Year`, `Month`, `Day` |
| `history.events[].article.blocks[].type` | `HistoryArticleBlockType` | `Heading`, `Paragraph`, `Quote`, `Image`, `Gallery`, `FactBox`, `SourceNote` |

## Rappel — enums vs résolution

Ce fichier liste les valeurs enum canoniques. Il ne garantit pas la résolution des propriétaires.

Même avec un enum valide (`ParkItem`, `Incident`, `Image`, etc.), le JSON doit fournir les IDs ou clés nécessaires :

- `ownerId` / `parkItemId` / `itemId` pour les événements de parkItems existants ;
- `ownerKey: "park"` pour une image du parc cible ;
- `ownerKey` et une entrée `items[]` correspondante pour une image de parkItem ;
- `ownerKey` préfixé et la référence correspondante dans le même JSON pour une image d’exploitant, de fondateur ou de constructeur ;
- une clé enregistrée ou un `ownerId` exact pour une image d’attraction autonome ;
- `imageId` pour les images déjà présentes dans l’export.

Pour les images de parkItems et de références, `ownerId` n’est pas utilisé comme solution de repli par le résolveur. Ne pas confondre une valeur enum valide ou un `ownerId` présent avec une relation réellement résolue.

Pour `history.events[].eventType`, utiliser la liste correspondant au propriétaire de l’événement.

### Événement d’histoire du parc

Valeurs `ParkHistoryEventType` pour `entityType: "Park"` :

- `Foundation`
- `Announcement`
- `ConstructionStart`
- `ConstructionMilestone`
- `Opening`
- `SeasonOpening`
- `Expansion`
- `AreaOpening`
- `AttractionOpening`
- `AttractionClosure`
- `Closure`
- `Reopening`
- `TemporaryClosure`
- `DefinitiveClosure`
- `Rename`
- `BrandingChange`
- `LogoChange`
- `OwnershipChange`
- `OperatorChange`
- `FounderMilestone`
- `Acquisition`
- `Sale`
- `Bankruptcy`
- `Liquidation`
- `LegalDispute`
- `Investment`
- `Masterplan`
- `InfrastructureChange`
- `TransportChange`
- `HotelOpening`
- `ResortExpansion`
- `ThemedAreaChange`
- `ParadeOrShowLaunch`
- `FestivalLaunch`
- `RecordOrAward`
- `AttendanceMilestone`
- `SafetyIncident`
- `Accident`
- `OperationalIncident`
- `WeatherEvent`
- `Fire`
- `Flood`
- `StormDamage`
- `HealthCrisis`
- `SecurityEvent`
- `StrikeOrSocialMovement`
- `RegulatoryChange`
- `PreservationOrHeritage`
- `Demolition`
- `Redevelopment`
- `MaintenanceCampaign`
- `TechnologyChange`
- `SustainabilityChange`
- `GuestExperienceChange`
- `PricingOrTicketingChange`
- `Partnership`
- `MediaAppearance`
- `Other`

### Événement d’histoire d’un parkItem

Valeurs `ParkItemHistoryEventType` pour `entityType: "ParkItem"` :

- `Announcement`
- `DesignStart`
- `ConstructionStart`
- `ConstructionMilestone`
- `TestingStart`
- `SoftOpening`
- `Opening`
- `SeasonOpening`
- `Closure`
- `TemporaryClosure`
- `DefinitiveClosure`
- `Reopening`
- `Refurbishment`
- `Rehab`
- `Retrack`
- `LayoutChange`
- `RideSystemChange`
- `CapacityChange`
- `TrainChange`
- `VehicleChange`
- `RestraintChange`
- `ManufacturerChange`
- `ModelChange`
- `Rename`
- `ThemeChange`
- `StoryChange`
- `LogoChange`
- `SponsorChange`
- `AccessibilityChange`
- `HeightRequirementChange`
- `QueueChange`
- `FastPassChange`
- `RelocationDeparture`
- `RelocationArrival`
- `Dismantling`
- `Storage`
- `Sale`
- `Acquisition`
- `Transfer`
- `Reinstallation`
- `Accident`
- `Incident`
- `SafetyModification`
- `Fire`
- `WeatherDamage`
- `TechnicalFailure`
- `OperationalChange`
- `RecordOrAward`
- `MediaAppearance`
- `PreservationOrHeritage`
- `Demolition`
- `Replacement`
- `Other`

## Contrôle final

Avant livraison d’un JSON :

- vérifier que toutes les valeurs enum utilisées sont présentes dans ce fichier ;
- vérifier séparément que chaque `attractionDetails.status` appartient au vocabulaire lifecycle contrôlé ;
- remplacer tout alias legacy par la valeur canonique actuelle ;
- corriger toute transformation historique rangée dans `attractionDetails.status` et la préserver dans `history.events` ;
- supprimer les valeurs devinées ou non documentées ;
- si une valeur manque, ne pas inventer une nouvelle enum : utiliser `Other` seulement quand le champ le prévoit et documenter la limite dans `metadata.notes`.
