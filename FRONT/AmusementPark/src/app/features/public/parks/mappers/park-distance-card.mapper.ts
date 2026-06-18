import { ParkDistanceTarget } from '@app/models/parks/park-distance';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { MeasurementSystem, DEFAULT_MEASUREMENT_SYSTEM } from '@shared/models/measurements/measurement-system.model';
import { MeasurementConversionService } from '@shared/services/measurements/measurement-conversion.service';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import { mapParkToCardModel } from '@shared/utils/mapping';

export function mapParkDistanceTargetToCardModel(
  target: ParkDistanceTarget,
  currentLanguage: string,
  countryDisplayService: CountryDisplayService | null = null,
  textTruncator: NaturalTextTruncatorService | null = null,
  measurementSystem: MeasurementSystem = DEFAULT_MEASUREMENT_SYSTEM,
  measurementConversionService: MeasurementConversionService = new MeasurementConversionService()
): ParkCardModel {
  return {
    ...mapParkToCardModel(target.park, currentLanguage, countryDisplayService, textTruncator),
    distanceLine: measurementConversionService.formatDistanceFromKilometers(target.distanceKilometers, measurementSystem, currentLanguage),
    travelDurationLine: formatTravelDuration(target.estimatedTravelDurationMinutes)
  };
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
