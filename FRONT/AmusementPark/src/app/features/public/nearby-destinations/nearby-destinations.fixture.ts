import { NearbyDestination } from '@app/models/parks/nearby-destination';

export function mixedNearbyDestinations(): NearbyDestination[] {
  return [{
    kind: 'StandaloneAttraction', park: null,
    standaloneAttraction: { id: 'alpine', name: 'Luge Alpine', type: 'Attraction', isVisible: true, adminReviewStatus: 'Validated',
      latitude: 49, longitude: 1, countryCode: 'FR', descriptions: [{ languageCode: 'fr', value: 'Une attraction dans la vallée. '.repeat(20) }] },
    distanceKilometers: 1.609344, estimatedTravelDurationMinutes: 10, proximityRank: 1
  }, {
    kind: 'Park', standaloneAttraction: null,
    park: { id: 'park', name: 'Parc voisin', latitude: 49, longitude: 2, countryCode: 'FR' },
    distanceKilometers: 3.218688, estimatedTravelDurationMinutes: 20, proximityRank: 2
  }];
}

