export interface ParkWeatherAttribution {
  providerName: string;
  providerUrl: string;
  licenseName: string;
  licenseUrl: string;
}

export interface ParkWeatherDay {
  localDate: string;
  dataKind: 'Forecast' | 'Observation' | string;
  weatherCode?: number | null;
  temperatureMinCelsius?: number | null;
  temperatureMaxCelsius?: number | null;
  apparentTemperatureMinCelsius?: number | null;
  apparentTemperatureMaxCelsius?: number | null;
  precipitationProbabilityMaxPercent?: number | null;
  precipitationSumMillimeters?: number | null;
  windSpeedMaxKilometersPerHour?: number | null;
  windGustsMaxKilometersPerHour?: number | null;
  timeZone?: string | null;
  fetchedAtUtc: string;
}

export interface ParkWeatherForecast {
  parkId: string;
  days: ParkWeatherDay[];
  attribution: ParkWeatherAttribution;
}
