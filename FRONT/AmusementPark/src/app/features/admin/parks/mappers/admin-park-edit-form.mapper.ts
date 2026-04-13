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
    founderId: [initialPark.founderId],
    operatorId: [initialPark.operatorId],
    latitude: [initialPark.latitude, Validators.required],
    longitude: [initialPark.longitude, Validators.required],
    isVisible: [initialPark.isVisible],
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
    founderId: park.founderId ?? null,
    operatorId: park.operatorId ?? null,
    latitude: park.latitude,
    longitude: park.longitude,
    isVisible: park.isVisible ?? true,
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
    founderId: normalizeOptionalString(rawValue['founderId']),
    operatorId: normalizeOptionalString(rawValue['operatorId']),
    latitude: Number(rawValue['latitude']),
    longitude: Number(rawValue['longitude']),
    isVisible: Boolean(rawValue['isVisible']),
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
    founderId: null,
    operatorId: null,
    latitude: DEFAULT_ADMIN_PARK_COORDINATES[0],
    longitude: DEFAULT_ADMIN_PARK_COORDINATES[1],
    isVisible: true,
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
