import { StandaloneAttractionMapPoint } from '@app/models/standalone-attractions/standalone-attraction-map-point';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { ParkMapPointViewModel } from '../models/park-map-point-view.model';
import { buildAddressLine, buildLocationLine, normalizeOptionalText } from './park-map-point-view.mapper';

export function mapStandaloneAttractionMapPointToViewModel(
  point: StandaloneAttractionMapPoint,
  currentLanguage: string,
  countryDisplayService: CountryDisplayService
): ParkMapPointViewModel | null {
  if (!point.id?.trim() || !point.name?.trim() || !Number.isFinite(point.latitude) || !Number.isFinite(point.longitude)) {
    return null;
  }

  const city: string | null = normalizeOptionalText(point.city);
  const countryCode: string | null = normalizeOptionalText(point.countryCode)?.toUpperCase() ?? null;
  const countryName: string | null = countryDisplayService.resolveLocalizedCountryName(countryCode, currentLanguage);
  const street: string | null = normalizeOptionalText(point.street);
  const postalCode: string | null = normalizeOptionalText(point.postalCode);

  return {
    kind: 'standaloneAttraction',
    id: point.id.trim(),
    name: point.name.trim(),
    countryCode,
    countryName,
    status: normalizeOptionalText(point.status),
    type: normalizeOptionalText(point.type),
    subtype: normalizeOptionalText(point.subtype),
    city,
    street,
    postalCode,
    latitude: point.latitude,
    longitude: point.longitude,
    locationLine: buildLocationLine(city, countryName ?? countryCode),
    addressLine: buildAddressLine(street, postalCode, city),
    coordinatesLine: `${point.latitude.toFixed(3)}, ${point.longitude.toFixed(3)}`,
    logoImageId: null
  };
}
