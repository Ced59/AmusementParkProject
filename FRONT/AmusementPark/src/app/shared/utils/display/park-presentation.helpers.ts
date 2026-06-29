import { Park } from '@app/models/parks/park';

export function buildParkSlug(value: string | null | undefined): string {
  return buildEntitySlug(value, 'park');
}

export function buildEntitySlug(value: string | null | undefined, fallback: string = ''): string {
  const slug: string = (value ?? '')
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .trim()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/(^-|-$)/g, '');

  return slug.length > 0 ? slug : fallback.trim();
}

export function buildParkLocationLine(park: Park | null | undefined, countryNameOverride: string | null = null): string | null {
  const countryLabel: string | null = countryNameOverride?.trim() || park?.countryCode?.trim() || null;
  const parts: string[] = [park?.city, countryLabel]
    .map((part: string | undefined | null): string => part?.trim() ?? '')
    .filter((part: string): boolean => part.length > 0);

  return parts.length > 0 ? parts.join(' · ') : null;
}

export function buildParkAddressLine(park: Park | null | undefined): string | null {
  const parts: string[] = [park?.street, park?.postalCode, park?.city]
    .map((part: string | undefined | null): string => part?.trim() ?? '')
    .filter((part: string): boolean => part.length > 0);

  return parts.length > 0 ? parts.join(', ') : null;
}
