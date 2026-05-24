import { Park } from '@app/models/parks/park';

export function buildParkSlug(value: string | null | undefined): string {
  return buildEntitySlug(value);
}

export function buildEntitySlug(value: string | null | undefined): string {
  return (value ?? '')
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .trim()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/(^-|-$)/g, '');
}

export function buildParkLocationLine(park: Park | null | undefined, countryNameOverride: string | null = null): string | null {
  const countryLabel: string | null = countryNameOverride?.trim() || park?.countryCode?.trim() || null;
  const parts: string[] = [park?.city, countryLabel]
    .filter((part: string | undefined | null): part is string => !!part?.trim());

  return parts.length > 0 ? parts.join(' · ') : null;
}

export function buildParkAddressLine(park: Park | null | undefined): string | null {
  const parts: string[] = [park?.street, park?.postalCode, park?.city]
    .filter((part: string | undefined | null): part is string => !!part?.trim());

  return parts.length > 0 ? parts.join(', ') : null;
}
