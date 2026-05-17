import { HomeFeaturedParkCardModel, HomeFeaturedParkMetricModel } from '@app/models/home/home-featured-park-card.model';
import { HomeFeaturedParkCategoryCountModel, HomeFeaturedParkModel } from '@app/models/home/home-featured-park.model';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import { resolveLocalizedCountryName } from '@shared/utils/display/country-display.helpers';
import { getParkItemCategoryTranslationKey, getParkTypeTranslationKey } from '@shared/utils/display/display-label.helpers';
import { buildParkSlug } from '@shared/utils/display/park-presentation.helpers';
import { resolveLocalizedValue, stripHtml } from '@shared/utils/localization';

type FeaturedParkTone = HomeFeaturedParkCardModel['tone'];

const TONES: readonly FeaturedParkTone[] = ['primary', 'sky', 'purple'];
const FEATURED_DESCRIPTION_MAX_LENGTH = 142;

export function mapHomeFeaturedParkToCardModel(
  park: HomeFeaturedParkModel,
  currentLanguage: string,
  truncator: NaturalTextTruncatorService,
  index: number
): HomeFeaturedParkCardModel {
  const plainDescription: string = stripHtml(resolveLocalizedValue(park.descriptions, currentLanguage) ?? null) ?? '';
  const countryName: string | null = resolveLocalizedCountryName(park.countryCode, currentLanguage);
  const city: string | null = normalizeOptionalString(park.city);
  const locationLine: string | null = buildLocationLine(city, countryName ?? park.countryCode ?? null);
  const normalizedName: string = normalizeOptionalString(park.name) ?? '';

  return {
    id: normalizeOptionalString(park.id),
    name: normalizedName,
    type: park.type ?? null,
    typeLabelKey: getParkTypeTranslationKey(park.type),
    city,
    countryCode: normalizeOptionalString(park.countryCode),
    countryName,
    locationLine,
    logoImageId: normalizeOptionalString(park.currentLogoImageId),
    description: truncator.truncate(plainDescription, { maxLength: FEATURED_DESCRIPTION_MAX_LENGTH }),
    metrics: buildMetrics(park.countsByCategory),
    isManualFeatured: Boolean(park.isManualFeatured),
    isSponsoredFeatured: Boolean(park.isSponsoredFeatured),
    detailLink: buildDetailLink(park.id, normalizedName, currentLanguage),
    tone: TONES[index % TONES.length]
  };
}

function buildMetrics(countsByCategory: HomeFeaturedParkCategoryCountModel[] | null | undefined): HomeFeaturedParkMetricModel[] {
  return (countsByCategory ?? [])
    .filter((count: HomeFeaturedParkCategoryCountModel) => Number.isFinite(count.count) && count.count > 0)
    .sort((left: HomeFeaturedParkCategoryCountModel, right: HomeFeaturedParkCategoryCountModel) => getCategorySortOrder(left.category) - getCategorySortOrder(right.category))
    .map((count: HomeFeaturedParkCategoryCountModel) => ({
      category: count.category,
      count: count.count,
      labelKey: getParkItemCategoryTranslationKey(count.category)
    }));
}

function getCategorySortOrder(category: ParkItemCategory): number {
  if (category === 'Attraction') {
    return 0;
  }

  return 10;
}

function buildDetailLink(parkId: string | null | undefined, parkName: string, currentLanguage: string): string[] | null {
  const normalizedParkId: string | null = normalizeOptionalString(parkId);
  if (!normalizedParkId || !parkName) {
    return null;
  }

  return ['/', currentLanguage, 'park', normalizedParkId, buildParkSlug(parkName)];
}

function buildLocationLine(city: string | null, countryName: string | null): string | null {
  const parts: string[] = [city, countryName]
    .filter((value: string | null): value is string => !!value && value.trim().length > 0);

  return parts.length > 0 ? parts.join(' • ') : null;
}

function normalizeOptionalString(value: string | null | undefined): string | null {
  const trimmedValue: string = value?.trim() ?? '';
  return trimmedValue.length > 0 ? trimmedValue : null;
}
