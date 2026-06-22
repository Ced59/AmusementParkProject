import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { ParkReferenceContactDetails } from '@app/models/parks/park-reference-contact-details';
import { LocalizedItem } from '@app/models/shared/localized-item';

type JsonObject = Record<string, unknown>;

export function buildManufacturerParkGraphUpsertJson(manufacturer: AttractionManufacturer): string {
  const document: JsonObject = {
    documentType: 'AmusementParkParkGraphUpsert',
    schemaVersion: '2026-05-25',
    mode: 'merge',
    identity: {
      name: '',
      countryCode: ''
    },
    references: {
      operators: [],
      founders: [],
      manufacturers: [
        buildManufacturerReference(manufacturer)
      ]
    },
    park: {},
    zones: [],
    items: [],
    images: [
      buildRemoteImageDraft(manufacturer)
    ],
    metadata: {
      source: 'admin-public-manufacturer-overlay',
      referenceKind: 'manufacturer',
      referenceId: normalizeOptionalString(manufacturer.id)
    }
  };

  return JSON.stringify(document, null, 2);
}

function buildRemoteImageDraft(manufacturer: AttractionManufacturer): JsonObject {
  return {
    sourceUrl: '',
    ownerKey: buildManufacturerKey(manufacturer),
    category: 'Manufacturer',
    description: '',
    isPublished: true,
    setAsCurrent: false,
    withWatermark: false
  };
}

export function buildManufacturerParkGraphUpsertFileName(manufacturer: AttractionManufacturer): string {
  const idOrName: string = normalizeOptionalString(manufacturer.id)
    ?? normalizeOptionalString(manufacturer.name)
    ?? 'manufacturer';
  const slug: string = idOrName
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');

  return `${slug || 'manufacturer'}-park-graph-upsert.json`;
}

function buildManufacturerReference(manufacturer: AttractionManufacturer): JsonObject {
  return {
    key: buildManufacturerKey(manufacturer),
    id: normalizeOptionalString(manufacturer.id),
    name: normalizeOptionalString(manufacturer.name) ?? '',
    legalName: normalizeOptionalString(manufacturer.legalName),
    foundedYear: normalizeOptionalNumber(manufacturer.foundedYear),
    closedYear: normalizeOptionalNumber(manufacturer.closedYear),
    contactDetails: buildContactDetails(manufacturer.contactDetails),
    biography: buildLocalizedItems(manufacturer.biography),
    adminReviewStatus: manufacturer.adminReviewStatus ?? 'ToReview'
  };
}

function buildManufacturerKey(manufacturer: AttractionManufacturer): string {
  const rawKey: string = normalizeOptionalString(manufacturer.id)
    ?? normalizeOptionalString(manufacturer.name)
    ?? 'manufacturer';
  const normalizedKey: string = rawKey
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');

  return `manufacturer:${normalizedKey || 'draft'}`;
}

function buildContactDetails(contactDetails: ParkReferenceContactDetails | null | undefined): JsonObject | null {
  if (!contactDetails) {
    return null;
  }

  return {
    websiteUrl: normalizeOptionalString(contactDetails.websiteUrl),
    email: normalizeOptionalString(contactDetails.email),
    phoneNumber: normalizeOptionalString(contactDetails.phoneNumber),
    street: normalizeOptionalString(contactDetails.street),
    city: normalizeOptionalString(contactDetails.city),
    postalCode: normalizeOptionalString(contactDetails.postalCode),
    countryCode: normalizeOptionalString(contactDetails.countryCode)?.toUpperCase() ?? null,
    latitude: normalizeOptionalNumber(contactDetails.latitude),
    longitude: normalizeOptionalNumber(contactDetails.longitude)
  };
}

function buildLocalizedItems(items: LocalizedItem<string>[] | null | undefined): JsonObject[] {
  return (items ?? [])
    .map((item: LocalizedItem<string>): JsonObject | null => {
      const languageCode: string | null = normalizeOptionalString(item.languageCode);
      if (!languageCode) {
        return null;
      }

      return {
        languageCode,
        value: item.value ?? ''
      };
    })
    .filter((item: JsonObject | null): item is JsonObject => item !== null);
}

function normalizeOptionalString(value: string | null | undefined): string | null {
  const trimmedValue: string = value?.trim() ?? '';
  return trimmedValue.length > 0 ? trimmedValue : null;
}

function normalizeOptionalNumber(value: number | null | undefined): number | null {
  return typeof value === 'number' && Number.isFinite(value) ? value : null;
}
