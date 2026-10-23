import { NearbyDestination } from '@app/models/parks/nearby-destination';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { MeasurementSystem } from '@shared/models/measurements/measurement-system.model';
import { MeasurementConversionService } from '@shared/services/measurements/measurement-conversion.service';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import { mapParkToCardModel } from '@shared/utils/mapping';
import { buildPublicParkRouteCommands, buildPublicStandaloneAttractionRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { getAttractionStatusValueKey } from '@shared/utils/display/park-item-presentation.helpers';
import { formatTravelDuration } from '../parks/mappers/park-distance-card.mapper';

export interface NearbyDestinationCard {
  card: ParkCardModel;
  detailLink: string[] | null;
}

export function mapNearbyDestinationToCard(
  target: NearbyDestination,
  language: string,
  countries: CountryDisplayService,
  truncator: NaturalTextTruncatorService,
  system: MeasurementSystem,
  measurements: MeasurementConversionService
): NearbyDestinationCard {
  const attraction = target.standaloneAttraction;
  const card = mapParkToCardModel(target.park ?? {
    id: attraction!.id ?? undefined,
    name: attraction!.name,
    countryCode: attraction!.countryCode ?? undefined,
    city: attraction!.city ?? undefined,
    street: attraction!.street ?? undefined,
    postalCode: attraction!.postalCode ?? undefined,
    webSiteUrl: attraction!.websiteUrl ?? undefined,
    latitude: attraction!.latitude!,
    longitude: attraction!.longitude!,
    descriptions: attraction!.descriptions
  }, language, countries, truncator);
  if (attraction) {
    card.photoImageId = target.photoImageId?.trim() || null;
    card.statusLabelKey = getAttractionStatusValueKey(attraction.attractionDetails?.status);
    card.statusIconClass = null;
    card.statusTone = 'soft';
  }
  card.distanceLine = measurements.formatDistanceFromKilometers(target.distanceKilometers, system, language);
  card.travelDurationLine = formatTravelDuration(target.estimatedTravelDurationMinutes);

  return {
    card,
    detailLink: attraction
      ? buildPublicStandaloneAttractionRouteCommands({ language, attractionId: card.id, attractionName: card.name })
      : buildPublicParkRouteCommands({ language, parkId: card.id, parkName: card.name })
  };
}
