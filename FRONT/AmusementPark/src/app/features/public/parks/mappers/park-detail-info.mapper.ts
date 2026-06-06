import { ParkType } from '@app/models/parks/park-type';
import { getLocalizedBooleanDisplay, getParkTypeTranslationKey } from '@shared/utils/display/display-label.helpers';
import { buildPublicParkReferenceRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { ParkDetailInfoRowViewModel } from '../models/park-detail-info-row.model';
import { ParkDetailStatViewModel } from '../models/park-detail-view.model';

export function buildIdentityRows(
  parkId: string | null,
  type: ParkType | null,
  founderName: string | null,
  operatorName: string | null,
  founderId: string | null,
  operatorId: string | null
): ParkDetailInfoRowViewModel[] {
  const rows: ParkDetailInfoRowViewModel[] = [];

  if (type) {
    rows.push({
      labelKey: 'parks.detail.identity.type',
      value: null,
      valueKey: getParkTypeTranslationKey(type),
      iconClass: 'pi pi-tag'
    });
  }

  if (founderName) {
    rows.push({
      labelKey: 'parks.detail.identity.founder',
      value: founderName,
      iconClass: 'pi pi-user',
      isMonospace: founderName === founderId
    });
  }

  if (operatorName) {
    rows.push({
      labelKey: 'parks.detail.identity.operator',
      value: operatorName,
      iconClass: 'pi pi-briefcase',
      isMonospace: operatorName === operatorId
    });
  }

  if (parkId) {
    rows.push({
      labelKey: 'parks.detail.identity.reference',
      value: parkId,
      iconClass: 'pi pi-hashtag',
      isMonospace: true
    });
  }

  return rows;
}

export function buildPracticalRows(
  countryName: string | null,
  city: string | null,
  postalCode: string | null,
  street: string | null,
  addressLine: string | null,
  websiteUrl: string | null,
  founderId: string | null,
  founderName: string | null,
  operatorId: string | null,
  operatorName: string | null,
  currentLanguage: string
): ParkDetailInfoRowViewModel[] {
  const rows: ParkDetailInfoRowViewModel[] = [];

  if (countryName) {
    rows.push({ labelKey: 'parks.fields.country', value: countryName, iconClass: 'pi pi-flag' });
  }

  if (city) {
    rows.push({ labelKey: 'parks.fields.city', value: city, iconClass: 'pi pi-building' });
  }

  if (postalCode) {
    rows.push({ labelKey: 'parks.detail.practical.postalCode', value: postalCode, iconClass: 'pi pi-inbox' });
  }

  if (street) {
    rows.push({ labelKey: 'parks.detail.practical.street', value: street, iconClass: 'pi pi-map-marker' });
  }

  if (addressLine && addressLine !== street) {
    rows.push({ labelKey: 'parks.fields.address', value: addressLine, iconClass: 'pi pi-map' });
  }

  if (websiteUrl) {
    rows.push({
      labelKey: 'parks.fields.website',
      value: websiteUrl,
      externalUrl: websiteUrl,
      iconClass: 'pi pi-external-link'
    });
  }

  if (operatorId) {
    const displayName: string = operatorName ?? operatorId;
    rows.push({
      labelKey: 'parks.detail.identity.operator',
      value: displayName,
      routerLink: buildPublicParkReferenceRouteCommands({
        language: currentLanguage,
        referenceId: operatorId,
        referenceName: displayName,
        kind: 'operator'
      }),
      iconClass: 'pi pi-briefcase',
      isMonospace: displayName === operatorId
    });
  }

  if (founderId) {
    const displayName: string = founderName ?? founderId;
    rows.push({
      labelKey: 'parks.detail.identity.founder',
      value: displayName,
      routerLink: buildPublicParkReferenceRouteCommands({
        language: currentLanguage,
        referenceId: founderId,
        referenceName: displayName,
        kind: 'founder'
      }),
      iconClass: 'pi pi-user',
      isMonospace: displayName === founderId
    });
  }

  return rows;
}

export function buildPublicationRows(
  isVisible: boolean | null,
  isFeaturedOnHome: boolean | null,
  isFeaturedOnHomeSponsored: boolean | null,
  featuredHomeOrder: number | null,
  logoImageId: string | null,
  currentLanguage: string
): ParkDetailInfoRowViewModel[] {
  const rows: ParkDetailInfoRowViewModel[] = [];

  if (isVisible != null) {
    rows.push({
      labelKey: 'parks.detail.publication.visible',
      value: getLocalizedBooleanDisplay(isVisible, currentLanguage),
      iconClass: isVisible ? 'pi pi-eye' : 'pi pi-eye-slash'
    });
  }

  if (isFeaturedOnHome != null) {
    rows.push({
      labelKey: 'parks.detail.publication.featured',
      value: getLocalizedBooleanDisplay(isFeaturedOnHome, currentLanguage),
      iconClass: 'pi pi-star'
    });
  }

  if (featuredHomeOrder != null) {
    rows.push({
      labelKey: 'parks.detail.publication.featuredOrder',
      value: featuredHomeOrder,
      iconClass: 'pi pi-sort-numeric-down'
    });
  }

  if (isFeaturedOnHomeSponsored != null) {
    rows.push({
      labelKey: 'parks.detail.publication.sponsored',
      value: getLocalizedBooleanDisplay(isFeaturedOnHomeSponsored, currentLanguage),
      iconClass: 'pi pi-megaphone'
    });
  }

  if (logoImageId) {
    rows.push({
      labelKey: 'parks.detail.publication.logo',
      value: 'parks.detail.publication.logoAvailable',
      valueKey: 'parks.detail.publication.logoAvailable',
      iconClass: 'pi pi-image'
    });
  }

  return rows;
}

export function buildLocationRows(hasLocationInfo: boolean, latitude: number, longitude: number): ParkDetailInfoRowViewModel[] {
  if (!hasLocationInfo) {
    return [];
  }

  return [
    { labelKey: 'parks.fields.latitude', value: latitude, iconClass: 'pi pi-compass' },
    { labelKey: 'parks.fields.longitude', value: longitude, iconClass: 'pi pi-compass' }
  ];
}

export function buildStats(
  totalItems: number,
  zoneCount: number,
  countryName: string | null,
  type: ParkType | null
): ParkDetailStatViewModel[] {
  const stats: ParkDetailStatViewModel[] = [
    {
      labelKey: 'parkVisitor.summary.totalItems',
      value: totalItems,
      hintKey: 'parkVisitor.summary.viewAllItems',
      tone: 'primary'
    }
  ];

  if (zoneCount > 0) {
    stats.push({
      labelKey: 'parks.detail.stats.zones',
      value: zoneCount,
      hintKey: 'parks.detail.zones.title',
      tone: 'lime'
    });
  }

  if (countryName) {
    stats.push({
      labelKey: 'parks.fields.country',
      value: countryName,
      hintKey: 'parks.detail.practical.title',
      tone: 'sky'
    });
  }

  if (type) {
    stats.push({
      labelKey: 'parks.detail.identity.type',
      value: '•',
      hintKey: getParkTypeTranslationKey(type),
      tone: 'gold'
    });
  }

  return stats.slice(0, 3);
}

export function normalizeOptionalString(value: string | null | undefined): string | null {
  const trimmedValue: string = value?.trim() ?? '';
  return trimmedValue.length > 0 ? trimmedValue : null;
}
