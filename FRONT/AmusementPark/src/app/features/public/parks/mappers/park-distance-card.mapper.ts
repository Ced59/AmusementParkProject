import { ParkDistanceTarget } from '@app/models/parks/park-distance';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { mapParkToCardModel } from '@shared/utils/mapping';

export function mapParkDistanceTargetToCardModel(
  target: ParkDistanceTarget,
  currentLanguage: string,
  countryDisplayService: CountryDisplayService | null = null
): ParkCardModel {
  return {
    ...mapParkToCardModel(target.park, currentLanguage, countryDisplayService),
    distanceLine: formatDistance(target.distanceKilometers),
    travelDurationLine: formatTravelDuration(target.estimatedTravelDurationMinutes)
  };
}

function formatDistance(distanceKilometers: number): string | null {
  if (!Number.isFinite(distanceKilometers) || distanceKilometers < 0) {
    return null;
  }

  if (distanceKilometers < 1) {
    return `${Math.round(distanceKilometers * 1000)} m`;
  }

  if (distanceKilometers < 10) {
    return `${distanceKilometers.toFixed(1)} km`;
  }

  return `${Math.round(distanceKilometers)} km`;
}

function formatTravelDuration(minutes: number): string | null {
  if (!Number.isFinite(minutes) || minutes <= 0) {
    return null;
  }

  if (minutes < 60) {
    return `~${minutes} min`;
  }

  const hours: number = Math.floor(minutes / 60);
  const remainingMinutes: number = minutes % 60;

  if (remainingMinutes === 0) {
    return `~${hours} h`;
  }

  return `~${hours} h ${remainingMinutes} min`;
}
