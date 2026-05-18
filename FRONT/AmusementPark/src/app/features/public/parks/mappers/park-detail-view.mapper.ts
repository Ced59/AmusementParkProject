import { Park } from '@app/models/parks/park';
import { ParkType } from '@app/models/parks/park-type';
import { buildEntitySlug, buildParkAddressLine, buildParkLocationLine, buildParkSlug } from '@shared/utils/display/park-presentation.helpers';
import { getLocalizedBooleanDisplay, getParkTypeTranslationKey } from '@shared/utils/display/display-label.helpers';
import { resolveLocalizedValue } from '@shared/utils/localization';
import { ParkDetailInfoRowViewModel } from '../models/park-detail-info-row.model';
import { ParkDetailStatViewModel, ParkDetailViewModel } from '../models/park-detail-view.model';

export interface ParkDetailReferenceNames {
  founderName?: string | null;
  operatorName?: string | null;
  countryName?: string | null;
}

export interface ParkDetailStatsSource {
  totalItems?: number | null;
  zoneCount?: number | null;
}

export function mapParkToDetailViewModel(
  park: Park,
  currentLanguage: string,
  references: ParkDetailReferenceNames = {},
  statsSource: ParkDetailStatsSource = {}
): ParkDetailViewModel {
  const hasLocationInfo: boolean = Number.isFinite(park.latitude) && Number.isFinite(park.longitude);
  const websiteUrl: string | null = normalizeOptionalString(park.webSiteUrl);
  const countryCode: string | null = normalizeOptionalString(park.countryCode);
  const countryName: string | null = normalizeOptionalString(references.countryName) ?? countryCode;
  const city: string | null = normalizeOptionalString(park.city);
  const street: string | null = normalizeOptionalString(park.street);
  const postalCode: string | null = normalizeOptionalString(park.postalCode);
  const addressLine: string | null = buildParkAddressLine(park);
  const locationLine: string | null = buildParkLocationLine(park, countryName);
  const logoImageId: string | null = normalizeOptionalString(park.currentLogoImageId);
  const type: ParkType | null = park.type ?? null;
  const founderId: string | null = normalizeOptionalString(park.founderId);
  const operatorId: string | null = normalizeOptionalString(park.operatorId);
  const founderName: string | null = normalizeOptionalString(references.founderName) ?? founderId;
  const operatorName: string | null = normalizeOptionalString(references.operatorName) ?? operatorId;
  const isVisible: boolean | null = park.isVisible ?? null;
  const isFeaturedOnHome: boolean | null = park.isFeaturedOnHome ?? null;
  const featuredHomeOrder: number | null = park.featuredHomeOrder ?? null;
  const isFeaturedOnHomeSponsored: boolean | null = park.isFeaturedOnHomeSponsored ?? null;
  const hasPracticalInfo: boolean = !!countryName || !!city || !!street || !!postalCode || !!websiteUrl || !!founderId || !!operatorId;
  const hasIdentity: boolean = !!park.id && !!park.name;
  const description: string | null = normalizeOptionalString(resolveLocalizedValue(park.descriptions, currentLanguage) ?? null);
  const totalItems: number = statsSource.totalItems ?? 0;
  const zoneCount: number = statsSource.zoneCount ?? 0;

  const identityRows: ParkDetailInfoRowViewModel[] = buildIdentityRows(park.id ?? null, type, founderName, operatorName, founderId, operatorId);
  const practicalRows: ParkDetailInfoRowViewModel[] = buildPracticalRows(
    countryName,
    city,
    postalCode,
    street,
    addressLine,
    websiteUrl,
    founderId,
    founderName,
    operatorId,
    operatorName,
    currentLanguage
  );
  const publicationRows: ParkDetailInfoRowViewModel[] = buildPublicationRows(
    isVisible,
    isFeaturedOnHome,
    isFeaturedOnHomeSponsored,
    featuredHomeOrder,
    logoImageId,
    currentLanguage
  );
  const locationRows: ParkDetailInfoRowViewModel[] = buildLocationRows(hasLocationInfo, park.latitude, park.longitude);
  const stats: ParkDetailStatViewModel[] = buildStats(totalItems, zoneCount, countryName, type);

  return {
    id: park.id ?? null,
    name: park.name?.trim() ?? '',
    countryCode,
    countryName,
    city,
    street,
    postalCode,
    websiteUrl,
    logoImageId,
    description,
    type,
    typeLabelKey: type ? getParkTypeTranslationKey(type) : null,
    founderId,
    founderName,
    operatorId,
    operatorName,
    isVisible,
    isFeaturedOnHome,
    featuredHomeOrder,
    isFeaturedOnHomeSponsored,
    locationLine,
    addressLine,
    latitude: hasLocationInfo ? park.latitude : null,
    longitude: hasLocationInfo ? park.longitude : null,
    hasPracticalInfo,
    hasLocationInfo,
    hasDescription: !!description,
    exploreLink: hasIdentity ? ['/', currentLanguage, 'park', park.id!, buildParkSlug(park.name!), 'items'] : null,
    identityRows,
    practicalRows,
    publicationRows,
    locationRows,
    stats,
  };
}

function buildIdentityRows(
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

function buildPracticalRows(
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
      routerLink: ['/', currentLanguage, 'park-operator', operatorId, buildEntitySlug(displayName)],
      iconClass: 'pi pi-briefcase',
      isMonospace: displayName === operatorId
    });
  }

  if (founderId) {
    const displayName: string = founderName ?? founderId;
    rows.push({
      labelKey: 'parks.detail.identity.founder',
      value: displayName,
      routerLink: ['/', currentLanguage, 'park-founder', founderId, buildEntitySlug(displayName)],
      iconClass: 'pi pi-user',
      isMonospace: displayName === founderId
    });
  }

  return rows;
}

function buildPublicationRows(
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

function buildLocationRows(hasLocationInfo: boolean, latitude: number, longitude: number): ParkDetailInfoRowViewModel[] {
  if (!hasLocationInfo) {
    return [];
  }

  return [
    { labelKey: 'parks.fields.latitude', value: latitude, iconClass: 'pi pi-compass' },
    { labelKey: 'parks.fields.longitude', value: longitude, iconClass: 'pi pi-compass' }
  ];
}

function buildStats(
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

function normalizeOptionalString(value: string | null | undefined): string | null {
  const trimmedValue: string = value?.trim() ?? '';
  return trimmedValue.length > 0 ? trimmedValue : null;
}
