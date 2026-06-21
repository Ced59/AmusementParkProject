# Enums - inventaire et reference JSON upsert

Date: 2026-06-21

Objectif: centraliser les enums du projet et expliciter les valeurs a utiliser dans les JSON upsert admin. Cette page complete l'audit historique `docs/architecture/enum-categorization-audit-2026-06-17.md`.

## Regles communes pour les JSON upsert

- Utiliser les noms canoniques en chaine, par exemple `"DropTower"` ou `"AdminReviewStatus": "ToReview"`.
- Ne pas envoyer les valeurs numeriques dans les JSON upsert. Les numeriques de `ParkItemType` restent documentes uniquement pour la compatibilite avec d'anciens payloads API.
- Le parseur accepte aujourd'hui certaines variantes de casse, d'espaces, de tirets ou d'underscores, mais ce sont des tolerances internes. La valeur contractuelle reste le nom enum exact.
- Les champs localises peuvent etre fournis pour toutes les langues supportees: `fr`, `en`, `es`, `de`, `it`, `pl`, `nl`, `pt`.
- Les valeurs enum ne sont pas localisees. Elles restent identiques quelle que soit la langue du bloc edite.

## Inventaire global

| Zone | Enums et unions de contrat |
| --- | --- |
| Parks domain | `ParkType`, `ParkStatus`, `ParkItemType`, `ParkItemCategory`, `ParkAdminSortField`, `ParkItemAdminSortField`, `AttractionWaterExposureLevel`, `AttractionAccessConditionUnit`, `AttractionAccessConditionType`, `AdminReviewStatus` |
| Images domain | `ImageOwnerType`, `ImageCategory` |
| Videos domain | `VideoHostingProvider`, `VideoOwnerType`, `VideoType` |
| Users domain | `Role`, `ExternalLoginProvider` |
| Weather domain | `ParkWeatherDataKind`, `ParkWeatherRefreshScope`, `ParkWeatherRunTrigger`, `ParkWeatherRunStatus`, `ParkWeatherRunItemStatus` |
| Ratings domain | `RatingTargetType` |
| Social share domain | `SocialShareTargetType`, `SocialShareChannel`, `SocialShareVisitorKind` |
| Application | `ApplicationErrorType`, `ClosedEntityFilter`, `WorldRegionFilter`, `LocalizedContentEntityType`, `ParkItemContentBacklogFilter`, `SitemapGenerationTrigger`, `SitemapGenerationStatus` |
| Web API DTO | `ParkTypeDto`, `ParkStatusDto`, `ParkItemTypeDto`, `ParkItemCategoryDto`, `AttractionWaterExposureLevelDto`, `AttractionAccessConditionUnitDto`, `AttractionAccessConditionTypeDto`, `AdminReviewStatusDto`, `ImageOwnerTypeDto`, `ImageCategoryDto`, `VideoHostingProviderDto`, `VideoOwnerTypeDto`, `VideoTypeDto`, `UserRoleDto`, `PublicCacheScope` |
| Frontend mirrors | `ParkType`, `ParkStatus`, `ParkItemType`, `ParkItemCategory`, `AdminReviewStatus`, `AttractionStatus`, `AttractionWaterExposureLevel`, `AttractionAccessConditionType`, `AttractionAccessConditionUnit`, `ImageOwnerType`, `ImageCategory`, `VideoHostingProvider`, `VideoOwnerType`, `VideoType`, `AppRole`, `RatingTargetType`, `ClosedEntityFilter`, `SeoSitemapGenerationStatus`, `ParkWeatherRunStatus` |

Les unions frontend purement UI comme les variantes visuelles, les tabs ou les modes de sauvegarde ne sont pas des contrats JSON upsert.

## JSON upsert park graph

Le document park graph est exporte par `ExportParkGraphJsonQueryHandler` avec `JsonStringEnumConverter`, donc les enums sortent deja sous forme de chaines.

| Enum | Champs JSON | Valeurs canoniques |
| --- | --- | --- |
| `ParkType` | `park.type` | `ThemePark`, `WaterPark`, `Zoo`, `AnimalPark`, `AmusementPark`, `Resort` |
| `ParkStatus` | `park.status` | `Operating`, `ClosedDefinitively` |
| `AdminReviewStatus` | `park.adminReviewStatus`, `references.operators[].adminReviewStatus`, `references.manufacturers[].adminReviewStatus`, `items[].adminReviewStatus` | `ToReview`, `Validated`, `ToProcessLater`, `NotRelevant` |
| `ParkItemCategory` | `items[].category` | `Attraction`, `Restaurant`, `Hotel`, `Animal`, `Show`, `Shop`, `Service`, `Transport`, `Other` |
| `ParkItemType` | `items[].type` | Voir la table dediee ci-dessous |
| `AttractionWaterExposureLevel` | `items[].attractionDetails.waterExposureLevel` | `None`, `Splash`, `Moderate`, `Soaking`, `ExtremeSoaking` |
| `AttractionAccessConditionType` | `items[].attractionDetails.accessConditions[].type` | `MinHeight`, `MinHeightAccompanied`, `MaxHeight`, `MinAge`, `MinAgeAccompanied`, `PregnancyRestriction`, `HeartRestriction`, `BackNeckRestriction`, `WheelchairTransferRequired`, `AccessPassRequired`, `Custom` |
| `AttractionAccessConditionUnit` | `items[].attractionDetails.accessConditions[].unit` | `Centimeter`, `Inch`, `Year` |
| `ImageOwnerType` | `images[].ownerType` | `Park`, `ParkItem`, `ParkOperator`, `ParkFounder`, `AttractionManufacturer` dans le park graph. Les autres valeurs domaine existent mais ne doivent pas etre utilisees dans ce flux. |
| `ImageCategory` | `images[].category` | `Avatar`, `ParkLogo`, `Park`, `ParkItem`, `Operator`, `Manufacturer`, `Founder`, `VideoThumbnail` |

`AdminReviewStatus.Ready` existe comme alias legacy de `Validated` cote domaine. Il ne doit pas etre utilise dans les nouveaux JSON.

## JSON upsert localized content

Le flux localized content applique un bloc cible via `LocalizedContentEntityType` dans la route ou le selecteur admin. Les valeurs d'entite sont: `Park`, `ParkZone`, `ParkItem`, `ParkOperator`, `ParkFounder`, `AttractionManufacturer`, `Image`, `ImageTag`, `AccessConditionType`.

Les champs multilingues acceptent ces deux formes:

```json
{
  "descriptions": {
    "fr": "Texte francais",
    "en": "English text"
  }
}
```

```json
{
  "descriptions": [
    { "languageCode": "fr", "value": "Texte francais" },
    { "languageCode": "en", "value": "English text" }
  ]
}
```

Les enums exploitables dans les champs bruts localized content sont:

| Enum | Entites / champs | Valeurs canoniques |
| --- | --- | --- |
| `ParkType` | `Park.type` | `ThemePark`, `WaterPark`, `Zoo`, `AnimalPark`, `AmusementPark`, `Resort` |
| `AdminReviewStatus` | `Park.adminReviewStatus`, `ParkItem.adminReviewStatus`, `ParkOperator.adminReviewStatus`, `AttractionManufacturer.adminReviewStatus` | `ToReview`, `Validated`, `ToProcessLater`, `NotRelevant` |
| `ParkItemCategory` | `ParkItem.category` | `Attraction`, `Restaurant`, `Hotel`, `Animal`, `Show`, `Shop`, `Service`, `Transport`, `Other` |
| `ParkItemType` | `ParkItem.type` | Voir la table dediee ci-dessous |
| `AttractionWaterExposureLevel` | `ParkItem.attractionDetails.waterExposureLevel` | `None`, `Splash`, `Moderate`, `Soaking`, `ExtremeSoaking` |
| `AttractionAccessConditionType` | `ParkItem.accessConditions[].type`, `AccessConditionType.legacyType` | `MinHeight`, `MinHeightAccompanied`, `MaxHeight`, `MinAge`, `MinAgeAccompanied`, `PregnancyRestriction`, `HeartRestriction`, `BackNeckRestriction`, `WheelchairTransferRequired`, `AccessPassRequired`, `Custom` |
| `AttractionAccessConditionUnit` | `ParkItem.accessConditions[].unit` | `Centimeter`, `Inch`, `Year` |

## ParkItemType

`ParkItemType` est utilise dans les JSON upsert park graph, dans localized content, dans les DTO HTTP et dans le miroir frontend `ParkItemType`.

| Valeur canonique | Valeur numerique stable | Categorie habituelle |
| --- | ---: | --- |
| `Attraction` | 0 | `Attraction` |
| `RollerCoaster` | 1 | `Attraction` |
| `WaterRide` | 2 | `Attraction` |
| `FlatRide` | 3 | `Attraction` |
| `DarkRide` | 4 | `Attraction` |
| `FamilyRide` | 5 | `Attraction` |
| `ThrillRide` | 6 | `Attraction` |
| `TransportRide` | 7 | `Attraction` |
| `WalkThrough` | 8 | `Attraction` |
| `Playground` | 9 | `Attraction` |
| `InteractiveExperience` | 10 | `Attraction` |
| `ObservationRide` | 11 | `Attraction` |
| `AnimalExhibit` | 12 | `Animal` |
| `Restaurant` | 13 | `Restaurant` |
| `Snack` | 14 | `Restaurant` |
| `Hotel` | 15 | `Hotel` |
| `Show` | 16 | `Show` |
| `Shop` | 17 | `Shop` |
| `Game` | 18 | `Attraction` |
| `MeetAndGreet` | 19 | `Attraction` |
| `Service` | 20 | `Service` |
| `Toilets` | 21 | `Service` |
| `FirstAid` | 22 | `Service` |
| `Information` | 23 | `Service` |
| `Locker` | 24 | `Service` |
| `Parking` | 25 | `Service` |
| `Transport` | 26 | `Transport` |
| `Station` | 27 | `Transport` |
| `Other` | 28 | `Other` |
| `Cinema` | 29 | `Attraction` |
| `DropTower` | 30 | `Attraction` |

Pour les attractions creees rapidement, seuls les types d'attraction sont acceptes avec `ParkItemCategory.Attraction`; un type incompatible est normalise vers le type par defaut de la categorie.
