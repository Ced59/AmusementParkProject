import { Park } from '@app/models/parks/park';
import { buildParkAddressLine, buildParkLocationLine, buildParkSlug } from '@shared/utils/display/park-presentation.helpers';
import { resolveLocalizedValue } from '@shared/utils/localization';
import { ParkDetailViewModel } from '../models/park-detail-view.model';

export function mapParkToDetailViewModel(park: Park, currentLanguage: string): ParkDetailViewModel {
  const hasLocationInfo: boolean = Number.isFinite(park.latitude) && Number.isFinite(park.longitude);
  const hasPracticalInfo: boolean = !!park.countryCode || !!park.city || !!park.street || !!park.postalCode || !!park.webSiteUrl;
  const hasIdentity: boolean = !!park.id && !!park.name;

  return {
    id: park.id ?? null,
    name: park.name?.trim() ?? '',
    countryCode: park.countryCode?.trim() ?? null,
    city: park.city?.trim() ?? null,
    street: park.street?.trim() ?? null,
    postalCode: park.postalCode?.trim() ?? null,
    websiteUrl: park.webSiteUrl?.trim() ?? null,
    logoImageId: park.currentLogoImageId?.trim() ?? null,
    description: resolveLocalizedValue(park.descriptions, currentLanguage) ?? null,
    locationLine: buildParkLocationLine(park),
    addressLine: buildParkAddressLine(park),
    latitude: hasLocationInfo ? park.latitude : null,
    longitude: hasLocationInfo ? park.longitude : null,
    hasPracticalInfo,
    hasLocationInfo,
    exploreLink: hasIdentity ? ['/', currentLanguage, 'park', park.id!, buildParkSlug(park.name!), 'items'] : null,
  };
}
