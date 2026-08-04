import { AttractionAccessCondition } from '@app/models/parks/attraction-access-condition';
import { AttractionAccessConditionUnit } from '@app/models/parks/attraction-access-condition-unit';
import { MeasurementSystem, DEFAULT_MEASUREMENT_SYSTEM } from '@shared/models/measurements/measurement-system.model';
import { MeasurementConversionService } from '@shared/services/measurements/measurement-conversion.service';
import { getLocalizedBooleanDisplay } from '@shared/utils/display/display-label.helpers';
import { findExactLocalizedText } from '@shared/utils/localization/localized-text.helpers';

interface LocalizedAgeUnits {
  one: string;
  few?: string;
  many?: string;
  other: string;
}

const LOCALIZED_AGE_UNITS: Record<string, LocalizedAgeUnits> = {
  de: { one: 'Jahr', other: 'Jahre' },
  en: { one: 'year', other: 'years' },
  es: { one: 'año', other: 'años' },
  fr: { one: 'an', other: 'ans' },
  it: { one: 'anno', other: 'anni' },
  nl: { one: 'jaar', other: 'jaar' },
  pl: { one: 'rok', few: 'lata', many: 'lat', other: 'lat' },
  pt: { one: 'ano', other: 'anos' }
};

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
  const languageCode: string = normalizeSupportedLanguageCode(currentLanguage);
  const units: LocalizedAgeUnits = LOCALIZED_AGE_UNITS[languageCode] ?? LOCALIZED_AGE_UNITS['en'];
  const pluralCategory: Intl.LDMLPluralRule = new Intl.PluralRules(languageCode).select(value);
  const suffix: string = units[pluralCategory as keyof LocalizedAgeUnits] ?? units.other;

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
  const localizedItem = findExactLocalizedText(items, normalizeSupportedLanguageCode(currentLanguage));
  const text: string = localizedItem?.value?.trim() ?? '';

  return text.length > 0 ? text : null;
}

function normalizeSupportedLanguageCode(languageCode: string): string {
  const normalizedLanguageCode: string = languageCode.trim().toLowerCase().split('-')[0];
  return LOCALIZED_AGE_UNITS[normalizedLanguageCode] ? normalizedLanguageCode : 'en';
}
