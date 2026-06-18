import { AttractionAccessCondition } from '@app/models/parks/attraction-access-condition';
import { AttractionAccessConditionUnit } from '@app/models/parks/attraction-access-condition-unit';
import { MeasurementSystem, DEFAULT_MEASUREMENT_SYSTEM } from '@shared/models/measurements/measurement-system.model';
import { MeasurementConversionService } from '@shared/services/measurements/measurement-conversion.service';
import { getLocalizedBooleanDisplay } from '@shared/utils/display/display-label.helpers';
import { resolveLocalizedText } from '@shared/utils/localization/localized-text.helpers';

const defaultMeasurementConversionService = new MeasurementConversionService();

export function trimOrNull(value: string | null | undefined): string | null {
  const trimmedValue: string = value?.trim() ?? '';
  return trimmedValue.length > 0 ? trimmedValue : null;
}

export function formatDate(value: string | null | undefined): string | null {
  const trimmedValue: string = value?.trim() ?? '';

  if (trimmedValue.length === 0) {
    return null;
  }

  return trimmedValue.length >= 10 ? trimmedValue.slice(0, 10) : trimmedValue;
}

export function formatNumberWithUnit(value: number | null | undefined, unit: string): string | null {
  if (value == null) {
    return null;
  }

  return `${formatNumber(value)} ${unit}`;
}

export function formatLengthFromMeters(
  value: number | null | undefined,
  currentLanguage: string,
  measurementSystem: MeasurementSystem = DEFAULT_MEASUREMENT_SYSTEM,
  measurementConversionService: MeasurementConversionService = defaultMeasurementConversionService
): string | null {
  return measurementConversionService.formatLengthFromMeters(value, measurementSystem, currentLanguage);
}

export function formatSpeedFromKilometersPerHour(
  value: number | null | undefined,
  currentLanguage: string,
  measurementSystem: MeasurementSystem = DEFAULT_MEASUREMENT_SYSTEM,
  measurementConversionService: MeasurementConversionService = defaultMeasurementConversionService
): string | null {
  return measurementConversionService.formatSpeedFromKilometersPerHour(value, measurementSystem, currentLanguage);
}

export function formatInteger(value: number | null | undefined): string | null {
  return value == null ? null : `${value}`;
}

export function formatDuration(value: number | null | undefined, currentLanguage: string): string | null {
  if (value == null) {
    return null;
  }

  if (value < 60) {
    return `${value} s`;
  }

  const minutes: number = Math.floor(value / 60);
  const seconds: number = value % 60;
  const minuteLabel: string = currentLanguage === 'fr' ? 'min' : 'min';

  if (seconds === 0) {
    return `${minutes} ${minuteLabel}`;
  }

  return `${minutes} ${minuteLabel} ${seconds} s`;
}

export function formatBoolean(value: boolean | null | undefined, currentLanguage: string): string | null {
  return getLocalizedBooleanDisplay(value, currentLanguage);
}

export function formatAccessConditionValue(
  value: number,
  unit: AttractionAccessConditionUnit | null | undefined,
  currentLanguage: string,
  measurementSystem: MeasurementSystem = DEFAULT_MEASUREMENT_SYSTEM,
  measurementConversionService: MeasurementConversionService = defaultMeasurementConversionService
): string {
  if (unit === 'Centimeter') {
    return measurementConversionService.formatAccessHeightFromCentimeters(value, measurementSystem, currentLanguage) ?? formatNumber(value);
  }

  if (unit === 'Inch') {
    const centimeters: number = measurementConversionService.inchesToCentimeters(value);
    return measurementConversionService.formatAccessHeightFromCentimeters(centimeters, measurementSystem, currentLanguage) ?? formatNumber(value);
  }

  if (unit === 'Year') {
    return formatAge(value, currentLanguage);
  }

  return formatNumber(value);
}

export function formatAge(value: number, currentLanguage: string): string {
  const suffix: string = currentLanguage === 'fr' ? 'ans' : 'years';
  return `${formatNumber(value)} ${suffix}`;
}

export function formatNumber(value: number): string {
  return Number.isInteger(value) ? `${value}` : `${value}`.replace('.', ',');
}

export function formatCoordinates(latitude: number, longitude: number, currentLanguage: string): string {
  const separator: string = currentLanguage === 'fr' ? ' · ' : ' · ';
  return `${latitude.toFixed(5)}${separator}${longitude.toFixed(5)}`;
}

export function isValidCoordinatePair(latitude: number | null | undefined, longitude: number | null | undefined): boolean {
  return latitude != null
    && longitude != null
    && Number.isFinite(latitude)
    && Number.isFinite(longitude)
    && Math.abs(latitude) <= 90
    && Math.abs(longitude) <= 180
    && !(latitude === 0 && longitude === 0);
}

export function resolveOptionalLocalizedText(items: AttractionAccessCondition['label'], currentLanguage: string): string | null {
  const text: string = resolveLocalizedText(items, currentLanguage, '');
  return text.trim().length > 0 ? text : null;
}
