# Enum and categorization audit - 2026-06-17

Scope: backend enum contracts and frontend enum/string-union mirrors used by park, park item, image and video categorization.

## Backend enums

Core/domain:

- `Parks`: `ParkType`, `ParkItemType`, `ParkItemCategory`, `ParkItemAdminSortField`, `AttractionWaterExposureLevel`, `AttractionAccessConditionUnit`, `AttractionAccessConditionType`, `AdminReviewStatus`.
- `Images`: `ImageOwnerType`, `ImageCategory`.
- `Videos`: `VideoHostingProvider`, `VideoOwnerType`, `VideoType`.
- `Users`: `Role`, `ExternalLoginProvider`.

Application/internal:

- `ApplicationErrorType`.
- `LocalizedContentEntityType`.
- `ParkItemContentBacklogFilter`.
- `WorldRegionFilter`.
- `SitemapGenerationTrigger`, `SitemapGenerationStatus`.

Web API DTOs:

- Park and park item DTO enums mirror the domain values: `ParkTypeDto`, `ParkItemTypeDto`, `ParkItemCategoryDto`, `AttractionWaterExposureLevelDto`, `AttractionAccessConditionUnitDto`, `AttractionAccessConditionTypeDto`, `AdminReviewStatusDto`.
- Image DTO enums mirror public image contracts: `ImageOwnerTypeDto`, `ImageCategoryDto`.
- Video DTO enums mirror public video contracts: `VideoHostingProviderDto`, `VideoOwnerTypeDto`, `VideoTypeDto`.
- User DTO enum: `UserRoleDto`.
- Web API infrastructure enum: `PublicCacheScope`.

## Frontend enums and string unions

TypeScript enums:

- `ImageOwnerType`, `ImageCategory`.
- `VideoHostingProvider`, `VideoOwnerType`, `VideoType`.
- `ViewState`.

String-union mirrors of backend enums:

- `ParkType`, `ParkItemType`, `ParkItemCategory`.
- `AdminReviewStatus`, `AppRole`.
- `AttractionAccessConditionType`, `AttractionAccessConditionUnit`, `AttractionWaterExposureLevel`, `AttractionStatus`.
- `SeoSitemapGenerationStatus`.

Local UI-only unions such as save modes, tabs, graph upsert wizard steps and map marker icon kinds were inspected and do not need backend harmonization.

## Decisions

- `ParkItemType.Cinema` / `ParkItemTypeDto.Cinema` / frontend `ParkItemType = 'Cinema'` were added for cinema and 4D cinema attractions.
- `ParkItemType` and `ParkItemTypeDto` now use explicit numeric values to keep legacy numeric payloads stable. `Other` remains `28`; `Cinema` is `29`.
- Entity-owner terminology is harmonized:
  - `ImageOwnerType.ParkItem` / `ImageOwnerTypeDto.PARK_ITEM` replace the former owner value named `Attraction`.
  - `VideoOwnerType.ParkItem` / `VideoOwnerTypeDto.PARK_ITEM` replace the former owner value named `Attraction`.
- Park item image categorization is harmonized:
  - `ImageCategory.ParkItem` / `ImageCategoryDto.PARK_ITEM` replace the former generic park item image category named `Attraction`.
- Backend domain obsolete aliases and Mongo repository filters are kept for `Attraction` owner/category values so existing Mongo string values remain readable during migration.
- The public HTTP DTO contract exposes `PARK_ITEM`; legacy `ATTRACTION` route values remain accepted only by image route parsing during the migration window.
- `AttractionManufacturer`, `ParkItemCategory.Attraction`, `ParkItemType.Attraction` and attraction details/access-condition enums remain unchanged because they model actual attraction concepts.
