import { ImageDto } from '@app/models/images/image-dto';
import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { ParkFounder } from '@app/models/parks/park-founder';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { ParkOperator } from '@app/models/parks/park-operator';
import { ParkReferenceContactDetails } from '@app/models/parks/park-reference-contact-details';
import { PaginationContract } from '@shared/models/contracts';
import { resolveLocalizedValue } from '@shared/utils/localization';
import { buildPublicParkItemRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import {
  buildPublicPhotoMetadata,
  buildPublicPhotoTagLookup,
  PublicPhotoMetadata,
  PublicPhotoTagLookup,
  UiPhotoCarouselCategoryOption,
  UiPhotoCarouselImage
} from '@ui/media';
import {
  ParkReferenceAttractionViewModel,
  ParkReferenceDetailViewModel,
  ParkReferenceFactViewModel
} from '../models/park-reference-detail-view.model';

export function mapParkFounderToReferenceDetailViewModel(
  founder: ParkFounder,
  currentLanguage: string,
  photos: ImageDto[] = []
): ParkReferenceDetailViewModel {
  const facts: ParkReferenceFactViewModel[] = [];
  addFact(facts, 'parks.reference.facts.occupation', founder.occupation, 'pi pi-id-card');
  addFact(facts, 'parks.reference.facts.birthDate', formatDate(founder.birthDate), 'pi pi-calendar');
  addFact(facts, 'parks.reference.facts.deathDate', formatDate(founder.deathDate), 'pi pi-calendar-times');
  addFact(facts, 'parks.reference.facts.birthPlace', founder.birthPlace, 'pi pi-map-marker');
  addFact(facts, 'parks.reference.facts.nationality', founder.nationalityCountryCode, 'pi pi-flag', null, false, true);
  addFact(facts, 'parks.reference.facts.website', founder.websiteUrl, 'pi pi-globe', normalizeWebsiteUrl(founder.websiteUrl), true);

  const gallery: UiPhotoCarouselImage[] = mapPhotos(photos, currentLanguage, founder.name, 'founder');

  return {
    id: founder.id ?? null,
    kind: 'founder',
    name: founder.name?.trim() ?? '',
    legalName: null,
    richDescription: normalizeOptionalString(resolveLocalizedValue(founder.biography, currentLanguage) ?? null),
    badgeKey: 'parks.reference.founder.badge',
    titleKey: 'parks.reference.founder.title',
    descriptionTitleKey: 'parks.reference.founder.descriptionTitle',
    emptyDescriptionKey: 'parks.reference.founder.emptyDescription',
    heroIconClass: 'pi pi-user',
    facts,
    photos: gallery,
    photoCategories: buildPhotoCategories(gallery),
    attractions: [],
    attractionsPagination: null
  };
}

export function mapParkOperatorToReferenceDetailViewModel(
  operator: ParkOperator,
  currentLanguage: string,
  photos: ImageDto[] = []
): ParkReferenceDetailViewModel {
  const facts: ParkReferenceFactViewModel[] = buildCompanyFacts(operator.legalName, operator.foundedYear, operator.closedYear, operator.contactDetails);
  const gallery: UiPhotoCarouselImage[] = mapPhotos(photos, currentLanguage, operator.name, 'operator');

  return {
    id: operator.id ?? null,
    kind: 'operator',
    name: operator.name?.trim() ?? '',
    legalName: normalizeOptionalString(operator.legalName),
    richDescription: normalizeOptionalString(resolveLocalizedValue(operator.description, currentLanguage) ?? null),
    badgeKey: 'parks.reference.operator.badge',
    titleKey: 'parks.reference.operator.title',
    descriptionTitleKey: 'parks.reference.operator.descriptionTitle',
    emptyDescriptionKey: 'parks.reference.operator.emptyDescription',
    heroIconClass: 'pi pi-briefcase',
    facts,
    photos: gallery,
    photoCategories: buildPhotoCategories(gallery),
    attractions: [],
    attractionsPagination: null
  };
}

export function mapAttractionManufacturerToReferenceDetailViewModel(
  manufacturer: AttractionManufacturer,
  currentLanguage: string,
  photos: ImageDto[] = [],
  attractions: ParkItemAdminRow[] = [],
  attractionsPagination: PaginationContract | null = null
): ParkReferenceDetailViewModel {
  const facts: ParkReferenceFactViewModel[] = buildCompanyFacts(
    manufacturer.legalName,
    manufacturer.foundedYear,
    manufacturer.closedYear,
    manufacturer.contactDetails
  );
  const gallery: UiPhotoCarouselImage[] = mapPhotos(photos, currentLanguage, manufacturer.name, 'manufacturer');

  return {
    id: manufacturer.id ?? null,
    kind: 'manufacturer',
    name: manufacturer.name?.trim() ?? '',
    legalName: normalizeOptionalString(manufacturer.legalName),
    richDescription: normalizeOptionalString(resolveLocalizedValue(manufacturer.biography, currentLanguage) ?? null),
    badgeKey: 'parks.reference.manufacturer.badge',
    titleKey: 'parks.reference.manufacturer.title',
    descriptionTitleKey: 'parks.reference.manufacturer.descriptionTitle',
    emptyDescriptionKey: 'parks.reference.manufacturer.emptyDescription',
    heroIconClass: 'pi pi-wrench',
    facts,
    photos: gallery,
    photoCategories: buildPhotoCategories(gallery),
    attractions: attractions.map((item: ParkItemAdminRow) => mapAttraction(item, currentLanguage)),
    attractionsPagination
  };
}

function buildCompanyFacts(
  legalName: string | null | undefined,
  foundedYear: number | null | undefined,
  closedYear: number | null | undefined,
  contactDetails: ParkReferenceContactDetails | null | undefined
): ParkReferenceFactViewModel[] {
  const facts: ParkReferenceFactViewModel[] = [];
  addFact(facts, 'parks.reference.facts.legalName', legalName, 'pi pi-building');
  addFact(facts, 'parks.reference.facts.foundedYear', formatYear(foundedYear), 'pi pi-calendar-plus');
  addFact(facts, 'parks.reference.facts.closedYear', formatYear(closedYear), 'pi pi-calendar-times');

  if (!contactDetails) {
    return facts;
  }

  addFact(facts, 'parks.reference.facts.website', contactDetails.websiteUrl, 'pi pi-globe', normalizeWebsiteUrl(contactDetails.websiteUrl), true);
  addFact(facts, 'parks.reference.facts.email', contactDetails.email, 'pi pi-envelope', normalizeEmailHref(contactDetails.email));
  addFact(facts, 'parks.reference.facts.phone', contactDetails.phoneNumber, 'pi pi-phone', normalizePhoneHref(contactDetails.phoneNumber));
  addFact(facts, 'parks.reference.facts.address', buildAddressLine(contactDetails), 'pi pi-map-marker');
  addFact(facts, 'parks.reference.facts.country', contactDetails.countryCode, 'pi pi-flag', null, false, true);
  addFact(facts, 'parks.reference.facts.coordinates', buildCoordinatesLine(contactDetails), 'pi pi-compass', null, false, true);

  return facts;
}

function mapAttraction(item: ParkItemAdminRow, currentLanguage: string): ParkReferenceAttractionViewModel {
  return {
    id: item.id,
    name: item.name,
    parkName: item.parkName,
    category: String(item.category),
    type: String(item.type),
    routerLink: buildPublicParkItemRouteCommands({
      language: currentLanguage,
      parkId: item.parkId,
      parkName: item.parkName,
      itemId: item.id,
      itemName: item.name
    })
  };
}

function mapPhotos(
  photos: ImageDto[],
  currentLanguage: string,
  ownerName: string,
  categoryKey: 'founder' | 'operator' | 'manufacturer'
): UiPhotoCarouselImage[] {
  const tagLookup: PublicPhotoTagLookup = buildPublicPhotoTagLookup([], currentLanguage);
  const categoryLabelKey: string = `parks.reference.photos.${categoryKey}`;

  return photos
    .filter((photo: ImageDto) => normalizeOptionalString(photo.id) !== null && photo.isPublished !== false)
    .map((photo: ImageDto): UiPhotoCarouselImage => {
      const metadata: PublicPhotoMetadata = buildPublicPhotoMetadata(photo, tagLookup, {
        currentLanguage,
        fallbackAlt: ownerName || 'Reference image',
        fallbackTagKey: categoryKey,
        fallbackTagLabelKey: categoryLabelKey
      });

      return {
        id: photo.id,
        imageId: photo.id,
        categoryKey,
        categoryLabelKey,
        ...metadata,
        isCurrent: photo.isCurrent,
        sourceTitle: ownerName,
        sourceSubtitle: photo.originalFileName ?? null,
        sourceIconClass: 'pi pi-image'
      };
    });
}

function buildPhotoCategories(photos: UiPhotoCarouselImage[]): UiPhotoCarouselCategoryOption[] {
  const counts: Record<string, number> = {};
  for (const photo of photos) {
    counts[photo.categoryKey] = (counts[photo.categoryKey] ?? 0) + 1;
  }

  return Object.entries(counts).map(([key, count]: [string, number]) => ({
    key,
    count,
    labelKey: `parks.reference.photos.${key}`
  }));
}

function addFact(
  facts: ParkReferenceFactViewModel[],
  labelKey: string,
  rawValue: string | number | null | undefined,
  iconClass: string,
  href: string | null = null,
  isExternal: boolean = false,
  isMonospace: boolean = false
): void {
  const value: string | null = typeof rawValue === 'number' ? String(rawValue) : normalizeOptionalString(rawValue);
  if (!value) {
    return;
  }

  facts.push({
    labelKey,
    value,
    iconClass,
    href,
    isExternal,
    isMonospace
  });
}

function buildAddressLine(contactDetails: ParkReferenceContactDetails): string | null {
  return [
    normalizeOptionalString(contactDetails.street),
    normalizeOptionalString(contactDetails.postalCode),
    normalizeOptionalString(contactDetails.city)
  ].filter((part: string | null): part is string => !!part).join(', ') || null;
}

function buildCoordinatesLine(contactDetails: ParkReferenceContactDetails): string | null {
  const latitude: number | null | undefined = contactDetails.latitude;
  const longitude: number | null | undefined = contactDetails.longitude;

  if (typeof latitude !== 'number' || typeof longitude !== 'number') {
    return null;
  }

  if (!Number.isFinite(latitude) || !Number.isFinite(longitude)) {
    return null;
  }

  return `${latitude}, ${longitude}`;
}

function formatDate(value: string | null | undefined): string | null {
  const normalizedValue: string | null = normalizeOptionalString(value);
  if (!normalizedValue) {
    return null;
  }

  return normalizedValue.length >= 10 ? normalizedValue.slice(0, 10) : normalizedValue;
}

function formatYear(value: number | null | undefined): string | null {
  return typeof value === 'number' && Number.isFinite(value) ? String(value) : null;
}

function normalizeWebsiteUrl(value: string | null | undefined): string | null {
  const normalizedValue: string | null = normalizeOptionalString(value);
  if (!normalizedValue) {
    return null;
  }

  return /^https?:\/\//i.test(normalizedValue) ? normalizedValue : `https://${normalizedValue}`;
}

function normalizeEmailHref(value: string | null | undefined): string | null {
  const normalizedValue: string | null = normalizeOptionalString(value);
  return normalizedValue ? `mailto:${normalizedValue}` : null;
}

function normalizePhoneHref(value: string | null | undefined): string | null {
  const normalizedValue: string | null = normalizeOptionalString(value);
  return normalizedValue ? `tel:${normalizedValue.replace(/\s+/g, '')}` : null;
}

function normalizeOptionalString(value: string | null | undefined): string | null {
  const trimmedValue: string = value?.trim() ?? '';
  return trimmedValue.length > 0 ? trimmedValue : null;
}
