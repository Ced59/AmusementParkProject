import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Park } from '@app/models/parks/park';
import { LocalizedItem } from '@app/models/shared/localized-item';

export const DEFAULT_ADMIN_PARK_COORDINATES: [number, number] = [48.8566, 2.3522];

export function createAdminParkEditForm(formBuilder: FormBuilder): FormGroup {
  const initialPark: Park = createDefaultAdminPark();

  return formBuilder.group({
    id: [initialPark.id],
    name: [initialPark.name, Validators.required],
    countryCode: [initialPark.countryCode],
    type: [initialPark.type],
    status: [initialPark.status ?? 'Operating'],
    founderId: [initialPark.founderId],
    operatorId: [initialPark.operatorId],
    openingDate: [initialPark.openingDate ?? ''],
    closingDate: [initialPark.closingDate ?? ''],
    openingDateText: [initialPark.openingDateText ?? ''],
    closingDateText: [initialPark.closingDateText ?? ''],
    latitude: [initialPark.latitude, Validators.required],
    longitude: [initialPark.longitude, Validators.required],
    isVisible: [initialPark.isVisible],
    adminReviewStatus: [initialPark.adminReviewStatus ?? 'Validated'],
    isFeaturedOnHome: [initialPark.isFeaturedOnHome ?? false],
    featuredHomeOrder: [initialPark.featuredHomeOrder ?? null],
    isFeaturedOnHomeSponsored: [initialPark.isFeaturedOnHomeSponsored ?? false],
    descriptions: [initialPark.descriptions ?? []],
    websiteUrl: [initialPark.webSiteUrl ?? ''],
    street: [initialPark.street ?? ''],
    city: [initialPark.city ?? ''],
    postalCode: [initialPark.postalCode ?? '']
  });
}

export function patchAdminParkEditForm(form: FormGroup, park: Park): void {
  form.patchValue({
    id: park.id,
    name: park.name ?? '',
    countryCode: park.countryCode ?? '',
    type: park.type ?? null,
    status: park.status ?? 'Operating',
    founderId: park.founderId ?? null,
    operatorId: park.operatorId ?? null,
    openingDate: normalizeDateForInput(park.openingDate),
    closingDate: normalizeDateForInput(park.closingDate),
    openingDateText: park.openingDateText ?? '',
    closingDateText: park.closingDateText ?? '',
    latitude: park.latitude,
    longitude: park.longitude,
    isVisible: park.isVisible ?? true,
    adminReviewStatus: park.adminReviewStatus ?? 'Validated',
    isFeaturedOnHome: park.isFeaturedOnHome ?? false,
    featuredHomeOrder: park.featuredHomeOrder ?? null,
    isFeaturedOnHomeSponsored: park.isFeaturedOnHomeSponsored ?? false,
    descriptions: park.descriptions ?? [],
    websiteUrl: park.webSiteUrl ?? '',
    street: park.street ?? '',
    city: park.city ?? '',
    postalCode: park.postalCode ?? ''
  }, { emitEvent: false });
}

export function mapAdminParkEditFormToPark(form: FormGroup): Park {
  const rawValue: Record<string, unknown> = form.getRawValue() as Record<string, unknown>;

  return {
    id: rawValue['id'] as string | undefined,
    name: rawValue['name'] as string,
    countryCode: normalizeOptionalString(rawValue['countryCode']) ?? undefined,
    type: (rawValue['type'] as Park['type']) ?? null,
    status: (rawValue['status'] as Park['status']) ?? 'Operating',
    founderId: normalizeOptionalString(rawValue['founderId']),
    operatorId: normalizeOptionalString(rawValue['operatorId']),
    openingDate: normalizeOptionalDate(rawValue['openingDate']),
    closingDate: normalizeOptionalDate(rawValue['closingDate']),
    openingDateText: normalizeOptionalString(rawValue['openingDateText']),
    closingDateText: normalizeOptionalString(rawValue['closingDateText']),
    latitude: Number(rawValue['latitude']),
    longitude: Number(rawValue['longitude']),
    isVisible: Boolean(rawValue['isVisible']),
    adminReviewStatus: (rawValue['adminReviewStatus'] as Park['adminReviewStatus']) ?? 'Validated',
    isFeaturedOnHome: Boolean(rawValue['isFeaturedOnHome']),
    featuredHomeOrder: normalizeOptionalNumber(rawValue['featuredHomeOrder']),
    isFeaturedOnHomeSponsored: Boolean(rawValue['isFeaturedOnHome']) && Boolean(rawValue['isFeaturedOnHomeSponsored']),
    descriptions: (rawValue['descriptions'] as LocalizedItem<string>[] | null | undefined) ?? [],
    webSiteUrl: normalizeOptionalString(rawValue['websiteUrl']) ?? undefined,
    street: normalizeOptionalString(rawValue['street']) ?? undefined,
    city: normalizeOptionalString(rawValue['city']) ?? undefined,
    postalCode: normalizeOptionalString(rawValue['postalCode']) ?? undefined
  } as Park;
}

export function buildAdminParkEditSnapshot(form: FormGroup): string {
  return JSON.stringify(mapAdminParkEditFormToPark(form));
}

export function getAdminParkFirstInvalidTabIndex(form: FormGroup): number {
  if (form.get('name')?.invalid) {
    return 0;
  }

  if (form.get('latitude')?.invalid || form.get('longitude')?.invalid) {
    return 1;
  }

  return 0;
}

function createDefaultAdminPark(): Park {
  return {
    id: undefined,
    name: '',
    countryCode: '',
    type: null,
    status: 'Operating',
    founderId: null,
    operatorId: null,
    openingDate: null,
    closingDate: null,
    openingDateText: null,
    closingDateText: null,
    latitude: DEFAULT_ADMIN_PARK_COORDINATES[0],
    longitude: DEFAULT_ADMIN_PARK_COORDINATES[1],
    isVisible: true,
    adminReviewStatus: 'Validated',
    isFeaturedOnHome: false,
    featuredHomeOrder: null,
    isFeaturedOnHomeSponsored: false,
    descriptions: []
  } as Park;
}

function normalizeOptionalString(value: unknown): string | null {
  if (typeof value !== 'string') {
    return null;
  }

  const trimmedValue: string = value.trim();
  return trimmedValue.length > 0 ? trimmedValue : null;
}

function normalizeOptionalNumber(value: unknown): number | null {
  if (value === null || value === undefined || value === '') {
    return null;
  }

  const normalizedValue: number = Number(value);
  return Number.isFinite(normalizedValue) && normalizedValue > 0 ? normalizedValue : null;
}

function normalizeDateForInput(value: unknown): string {
  const normalizedValue: string = String(value ?? '').trim();

  if (normalizedValue.length === 0) {
    return '';
  }

  const isoDateMatch: RegExpMatchArray | null = normalizedValue.match(/^(\d{4}-\d{2}-\d{2})/);
  if (isoDateMatch) {
    return isoDateMatch[1];
  }

  const parsedDate: Date = new Date(normalizedValue);
  if (Number.isNaN(parsedDate.getTime())) {
    return '';
  }

  const year: string = String(parsedDate.getUTCFullYear()).padStart(4, '0');
  const month: string = String(parsedDate.getUTCMonth() + 1).padStart(2, '0');
  const day: string = String(parsedDate.getUTCDate()).padStart(2, '0');

  return `${year}-${month}-${day}`;
}

function normalizeOptionalDate(value: unknown): string | null {
  const normalizedValue: string = normalizeDateForInput(value);
  return normalizedValue.length > 0 ? normalizedValue : null;
}
