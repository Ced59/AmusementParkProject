# AmusementPark — Enums JSON Park Graph Upsert

Version : **2026-06-30**

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
| `park.status` | `ParkStatus` | `Operating`, `ClosedDefinitively` |
| `park.adminReviewStatus` | `AdminReviewStatus` | `ToReview`, `Validated`, `ToProcessLater`, `NotRelevant` |

`AdminReviewStatus.Ready` existe comme alias legacy de `Validated`, mais ne doit pas être utilisé dans les nouveaux JSON.

## Champs parkItems

| Champ JSON | Enum | Valeurs |
| --- | --- | --- |
| `items[].category` | `ParkItemCategory` | `Attraction`, `Restaurant`, `Hotel`, `Animal`, `Show`, `Shop`, `Service`, `Transport`, `Other` |
| `items[].type` | `ParkItemType` | `Attraction`, `RollerCoaster`, `WaterRide`, `FlatRide`, `DarkRide`, `FamilyRide`, `ThrillRide`, `TransportRide`, `WalkThrough`, `Playground`, `InteractiveExperience`, `ObservationRide`, `AnimalExhibit`, `Restaurant`, `Snack`, `Hotel`, `Show`, `Shop`, `Game`, `MeetAndGreet`, `Service`, `Toilets`, `FirstAid`, `Information`, `Locker`, `Parking`, `Transport`, `Station`, `Other`, `Cinema`, `DropTower` |
| `items[].adminReviewStatus` | `AdminReviewStatus` | `ToReview`, `Validated`, `ToProcessLater`, `NotRelevant` |
| `items[].attractionDetails.waterExposureLevel` | `AttractionWaterExposureLevel` | `None`, `Splash`, `Moderate`, `Soaking`, `ExtremeSoaking` |
| `items[].attractionDetails.accessConditions[].type` | `AttractionAccessConditionType` | `MinHeight`, `MinHeightAccompanied`, `MaxHeight`, `MinAge`, `MinAgeAccompanied`, `PregnancyRestriction`, `HeartRestriction`, `BackNeckRestriction`, `WheelchairTransferRequired`, `AccessPassRequired`, `Custom` |
| `items[].attractionDetails.accessConditions[].unit` | `AttractionAccessConditionUnit` | `Centimeter`, `Inch`, `Year` |

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
- `ownerId` pour les images d’entités existantes ;
- `imageId` pour les images déjà présentes dans l’export.

Ne pas confondre une valeur enum valide avec une relation résolue.

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
- remplacer tout alias legacy par la valeur canonique actuelle ;
- supprimer les valeurs devinées ou non documentées ;
- si une valeur manque, ne pas inventer une nouvelle enum : utiliser `Other` seulement quand le champ le prévoit et documenter la limite dans `metadata.notes`.
