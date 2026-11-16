import { NearbyDestination } from '@app/models/parks/nearby-destination';
import { NearbyDestinationCardResponse } from '@app/models/parks/nearby-destination-card-response';

export function mapNearbyDestinationResponse(response: NearbyDestinationCardResponse | NearbyDestination): NearbyDestination {
  // Older APIs ignore cardView during rolling deployments and return the legacy shape.
  if ('park' in response) {
    return response;
  }
  const common = {
    id: response.id, name: response.name ?? '', countryCode: response.countryCode ?? undefined,
    city: response.city ?? undefined, street: response.street ?? undefined,
    postalCode: response.postalCode ?? undefined,
    latitude: response.latitude ?? undefined, longitude: response.longitude ?? undefined
  };
  const distance = {
    shortDescriptions: response.shortDescriptions,
    distanceKilometers: response.distanceKilometers,
    estimatedTravelDurationMinutes: response.estimatedTravelDurationMinutes,
    proximityRank: response.proximityRank, photoImageId: response.photoImageId
  };
  if (response.kind === 'Park') {
    return { ...distance, kind: 'Park', standaloneAttraction: null, park: {
      ...common, latitude: response.latitude!, longitude: response.longitude!,
      status: response.parkStatus ?? undefined, currentLogoImageId: response.logoImageId,
      webSiteUrl: response.websiteUrl ?? undefined
    } };
  }
  return { ...distance, kind: 'StandaloneAttraction', park: null, standaloneAttraction: {
    ...common, websiteUrl: response.websiteUrl,
    attractionDetails: { status: response.attractionStatus }
  } };
}
