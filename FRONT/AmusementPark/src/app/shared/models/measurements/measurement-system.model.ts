export type MeasurementSystem = 'Metric' | 'Imperial';

export const DEFAULT_MEASUREMENT_SYSTEM: MeasurementSystem = 'Metric';

export function normalizeMeasurementSystem(value: string | null | undefined): MeasurementSystem {
  const normalizedValue: string = value?.trim().toLowerCase() ?? '';

  return normalizedValue === 'imperial' ? 'Imperial' : DEFAULT_MEASUREMENT_SYSTEM;
}
