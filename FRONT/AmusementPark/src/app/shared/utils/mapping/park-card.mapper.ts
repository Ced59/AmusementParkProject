import { Park } from '@app/models/parks/park';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import { buildParkAddressLine, buildParkLocationLine } from '@shared/utils/display/park-presentation.helpers';
import { resolveLocalizedCountryName } from '@shared/utils/display/country-display.helpers';
import { resolveLocalizedValue, stripHtml } from '@shared/utils/localization';

const PARK_CARD_DESCRIPTION_MAX_LENGTH = 140;

export function mapParkToCardModel(
  park: Park,
  currentLanguage: string,
  countryDisplayService: CountryDisplayService | null = null,
  textTruncator: NaturalTextTruncatorService | null = null
): ParkCardModel {
  const shortDescription: string | null = buildShortDescription(
    resolveLocalizedValue(park.descriptions, currentLanguage) ?? null,
    textTruncator
  );
  const hasCoordinates: boolean = Number.isFinite(park.latitude) && Number.isFinite(park.longitude);
  const countryName: string | null = countryDisplayService?.resolveLocalizedCountryName(park.countryCode, currentLanguage)
    ?? resolveLocalizedCountryName(park.countryCode, currentLanguage);

  return {
    id: park.id ?? null,
    name: park.name?.trim() ?? '',
    countryCode: park.countryCode?.trim() ?? null,
    city: park.city?.trim() ?? null,
    status: park.status ?? 'Operating',
    latitude: hasCoordinates ? park.latitude : null,
    longitude: hasCoordinates ? park.longitude : null,
    logoImageId: park.currentLogoImageId?.trim() ?? null,
    websiteUrl: park.webSiteUrl?.trim() ?? null,
    locationLine: buildParkLocationLine(park, countryName),
    addressLine: buildParkAddressLine(park),
    coordinatesLine: hasCoordinates ? `${park.latitude.toFixed(3)}, ${park.longitude.toFixed(3)}` : null,
    shortDescription,
    isClosedDefinitively: park.status === 'ClosedDefinitively'
  };
}

function buildShortDescription(value: string | null, textTruncator: NaturalTextTruncatorService | null): string | null {
  const plainText: string = stripHtml(value);

  if (!plainText) {
    return null;
  }

  if (textTruncator) {
    return textTruncator.truncate(plainText, { maxLength: PARK_CARD_DESCRIPTION_MAX_LENGTH, ellipsis: '...' });
  }

  if (plainText.length <= PARK_CARD_DESCRIPTION_MAX_LENGTH) {
    return plainText;
  }

  return `${plainText.slice(0, PARK_CARD_DESCRIPTION_MAX_LENGTH - 3).trimEnd()}...`;
}
