import { Injectable } from '@angular/core';

import { MeasurementSystem, normalizeMeasurementSystem } from '@shared/models/measurements/measurement-system.model';

@Injectable({
  providedIn: 'root'
})
export class MeasurementConversionService {
  private static readonly metersPerFoot: number = 0.3048;
  private static readonly kilometersPerMile: number = 1.609344;
  private static readonly centimetersPerInch: number = 2.54;

  feetToMeters(feet: number): number {
    return roundMetric(feet * MeasurementConversionService.metersPerFoot);
  }

  metersToFeet(meters: number): number {
    return roundImperial(meters / MeasurementConversionService.metersPerFoot);
  }

  milesPerHourToKilometersPerHour(milesPerHour: number): number {
    return roundMetric(milesPerHour * MeasurementConversionService.kilometersPerMile);
  }

  kilometersPerHourToMilesPerHour(kilometersPerHour: number): number {
    return roundImperial(kilometersPerHour / MeasurementConversionService.kilometersPerMile);
  }

  inchesToCentimeters(inches: number): number {
    return roundMetric(inches * MeasurementConversionService.centimetersPerInch);
  }

  centimetersToInches(centimeters: number): number {
    return roundImperial(centimeters / MeasurementConversionService.centimetersPerInch);
  }

  formatLengthFromMeters(value: number | null | undefined, system: MeasurementSystem, language: string): string | null {
    if (!isFinitePositiveOrZero(value)) {
      return null;
    }

    const normalizedSystem: MeasurementSystem = normalizeMeasurementSystem(system);
    if (normalizedSystem === 'Imperial') {
      return `${formatLocalizedNumber(this.metersToFeet(value), language, 0)} ft`;
    }

    return `${formatLocalizedNumber(value, language, Number.isInteger(value) ? 0 : 1)} m`;
  }

  formatSpeedFromKilometersPerHour(value: number | null | undefined, system: MeasurementSystem, language: string): string | null {
    if (!isFinitePositiveOrZero(value)) {
      return null;
    }

    const normalizedSystem: MeasurementSystem = normalizeMeasurementSystem(system);
    if (normalizedSystem === 'Imperial') {
      return `${formatLocalizedNumber(this.kilometersPerHourToMilesPerHour(value), language, 0)} mph`;
    }

    return `${formatLocalizedNumber(value, language, Number.isInteger(value) ? 0 : 1)} km/h`;
  }

  formatAccessHeightFromCentimeters(value: number | null | undefined, system: MeasurementSystem, language: string): string | null {
    if (!isFinitePositiveOrZero(value)) {
      return null;
    }

    const normalizedSystem: MeasurementSystem = normalizeMeasurementSystem(system);
    if (normalizedSystem === 'Imperial') {
      return formatFeetAndInches(this.centimetersToInches(value));
    }

    return `${formatLocalizedNumber(value, language, Number.isInteger(value) ? 0 : 1)} cm`;
  }

  formatDistanceFromKilometers(value: number | null | undefined, system: MeasurementSystem, language: string): string | null {
    if (!isFinitePositiveOrZero(value)) {
      return null;
    }

    const normalizedSystem: MeasurementSystem = normalizeMeasurementSystem(system);
    if (normalizedSystem === 'Imperial') {
      const miles: number = value / MeasurementConversionService.kilometersPerMile;

      if (miles < 0.1) {
        return `${formatLocalizedNumber(value * 3280.839895, language, 0)} ft`;
      }

      if (miles < 10) {
        return `${formatLocalizedNumber(miles, language, 1)} mi`;
      }

      return `${formatLocalizedNumber(miles, language, 0)} mi`;
    }

    if (value < 1) {
      return `${formatLocalizedNumber(value * 1000, language, 0)} m`;
    }

    if (value < 10) {
      return `${formatLocalizedNumber(value, language, 1)} km`;
    }

    return `${formatLocalizedNumber(value, language, 0)} km`;
  }
}

function isFinitePositiveOrZero(value: number | null | undefined): value is number {
  return value != null && Number.isFinite(value) && value >= 0;
}

function roundMetric(value: number): number {
  return Math.round(value * 100) / 100;
}

function roundImperial(value: number): number {
  return Math.round(value * 100) / 100;
}

function formatFeetAndInches(inches: number): string {
  const totalInches: number = Math.max(0, Math.round(inches));
  const feet: number = Math.floor(totalInches / 12);
  const remainingInches: number = totalInches % 12;

  if (feet <= 0) {
    return `${remainingInches} in`;
  }

  if (remainingInches === 0) {
    return `${feet} ft`;
  }

  return `${feet} ft ${remainingInches} in`;
}

function formatLocalizedNumber(value: number, language: string, maximumFractionDigits: number): string {
  return new Intl.NumberFormat(language || 'en', {
    maximumFractionDigits,
    minimumFractionDigits: 0
  }).format(value);
}
