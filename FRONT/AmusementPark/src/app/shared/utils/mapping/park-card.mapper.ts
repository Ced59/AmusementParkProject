import { Park } from '@app/models/parks/park';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { buildParkAddressLine, buildParkLocationLine } from '@shared/utils/display/park-presentation.helpers';
import { resolveLocalizedValue, stripHtml } from '@shared/utils/localization';

export function mapParkToCardModel(park: Park, currentLanguage: string): ParkCardModel {
  const shortDescription: string | null = buildShortDescription(
    resolveLocalizedValue(park.descriptions, currentLanguage) ?? null
  );
  const hasCoordinates: boolean = Number.isFinite(park.latitude) && Number.isFinite(park.longitude);

  return {
    id: park.id ?? null,
    name: park.name?.trim() ?? '',
    countryCode: park.countryCode?.trim() ?? null,
    city: park.city?.trim() ?? null,
    latitude: hasCoordinates ? park.latitude : null,
    longitude: hasCoordinates ? park.longitude : null,
    logoImageId: park.currentLogoImageId?.trim() ?? null,
    websiteUrl: park.webSiteUrl?.trim() ?? null,
    locationLine: buildParkLocationLine(park),
    addressLine: buildParkAddressLine(park),
    coordinatesLine: hasCoordinates ? `${park.latitude.toFixed(3)}, ${park.longitude.toFixed(3)}` : null,
    shortDescription,
  };
}

function buildShortDescription(value: string | null): string | null {
  const plainText: string = stripHtml(value);

  if (!plainText) {
    return null;
  }

  if (plainText.length <= 140) {
    return plainText;
  }

  return `${plainText.slice(0, 137).trimEnd()}...`;
}
