import { ParkDistanceTarget } from '@app/models/parks/park-distance';

import { mapParkDistanceTargetToCardModel } from './park-distance-card.mapper';

describe('mapParkDistanceTargetToCardModel', () => {
  function createTarget(distanceKilometers: number, minutes: number): ParkDistanceTarget {
    return {
      proximityRank: 1,
      distanceKilometers,
      distanceMeters: distanceKilometers * 1000,
      distanceUnit: 'km',
      estimatedTravelDurationMinutes: minutes,
      park: {
        id: 'park-1',
        name: 'Park',
        latitude: 50,
        longitude: 3,
        descriptions: []
      }
    };
  }

  it('formats meter, decimal kilometer and rounded kilometer distances', () => {
    expect(mapParkDistanceTargetToCardModel(createTarget(0.45, 10), 'en').distanceLine).toBe('450 m');
    expect(mapParkDistanceTargetToCardModel(createTarget(4.26, 10), 'en').distanceLine).toBe('4.3 km');
    expect(mapParkDistanceTargetToCardModel(createTarget(12.7, 10), 'en').distanceLine).toBe('13 km');
  });

  it('returns null distance lines for invalid distances', () => {
    expect(mapParkDistanceTargetToCardModel(createTarget(-1, 10), 'en').distanceLine).toBeNull();
    expect(mapParkDistanceTargetToCardModel(createTarget(Number.NaN, 10), 'en').distanceLine).toBeNull();
  });

  it('formats travel durations below, equal to and above one hour', () => {
    expect(mapParkDistanceTargetToCardModel(createTarget(1, 45), 'en').travelDurationLine).toBe('~45 min');
    expect(mapParkDistanceTargetToCardModel(createTarget(1, 60), 'en').travelDurationLine).toBe('~1 h');
    expect(mapParkDistanceTargetToCardModel(createTarget(1, 95), 'en').travelDurationLine).toBe('~1 h 35 min');
  });

  it('returns null travel duration lines for zero or invalid durations', () => {
    expect(mapParkDistanceTargetToCardModel(createTarget(1, 0), 'en').travelDurationLine).toBeNull();
    expect(mapParkDistanceTargetToCardModel(createTarget(1, Number.NaN), 'en').travelDurationLine).toBeNull();
  });
});
