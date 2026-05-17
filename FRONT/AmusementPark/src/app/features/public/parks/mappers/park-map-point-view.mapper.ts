import { ParkMapPoint } from '@app/models/parks/park-map-point';
import { ParkMapPointViewModel } from '../models/park-map-point-view.model';

export function mapParkMapPointToViewModel(point: ParkMapPoint): ParkMapPointViewModel | null {
  const hasIdentifier: boolean = typeof point.id === 'string' && point.id.trim().length > 0;
  const hasName: boolean = typeof point.name === 'string' && point.name.trim().length > 0;
  const hasCoordinates: boolean = Number.isFinite(point.latitude) && Number.isFinite(point.longitude);

  if (!hasIdentifier || !hasName || !hasCoordinates) {
    return null;
  }

  const city: string | null = normalizeOptionalText(point.city);
  const countryCode: string | null = normalizeOptionalText(point.countryCode)?.toUpperCase() ?? null;
  const street: string | null = normalizeOptionalText(point.street);
  const postalCode: string | null = normalizeOptionalText(point.postalCode);

  return {
    id: point.id.trim(),
    name: point.name.trim(),
    countryCode,
    city,
    street,
    postalCode,
    latitude: point.latitude,
    longitude: point.longitude,
    locationLine: buildLocationLine(city, countryCode),
    addressLine: buildAddressLine(street, postalCode, city),
    coordinatesLine: `${point.latitude.toFixed(3)}, ${point.longitude.toFixed(3)}`,
    logoImageId: normalizeOptionalText(point.currentLogoImageId),
  };
}

function normalizeOptionalText(value: string | null | undefined): string | null {
  const normalizedValue: string = value?.trim() ?? '';
  return normalizedValue.length > 0 ? normalizedValue : null;
}

function buildLocationLine(city: string | null, countryCode: string | null): string | null {
  const parts: string[] = [city, countryCode].filter((part: string | null): part is string => !!part);
  return parts.length > 0 ? parts.join(' · ') : null;
}

function buildAddressLine(street: string | null, postalCode: string | null, city: string | null): string | null {
  const postalCity: string | null = [postalCode, city].filter((part: string | null): part is string => !!part).join(' ') || null;
  const parts: string[] = [street, postalCity].filter((part: string | null): part is string => !!part);
  return parts.length > 0 ? parts.join(', ') : null;
}
