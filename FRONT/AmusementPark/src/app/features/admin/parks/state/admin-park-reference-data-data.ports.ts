import { inject, InjectionToken } from '@angular/core';
import { CountriesApiService } from '@data-access/countries/countries-api.service';
import { ParkFoundersApiService } from '@data-access/parks/park-founders-api.service';
import { ParkOperatorsApiService } from '@data-access/parks/park-operators-api.service';

export interface AdminParkReferenceDataCountriesApiServicePort extends Pick<CountriesApiService, 'getCountries'> {
}

export const ADMIN_PARK_REFERENCE_DATA_COUNTRIES_API_SERVICE_PORT = new InjectionToken<AdminParkReferenceDataCountriesApiServicePort>('ADMIN_PARK_REFERENCE_DATA_COUNTRIES_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(CountriesApiService)
});

export interface AdminParkReferenceDataParkFoundersApiServicePort extends Pick<ParkFoundersApiService, 'getParkFounders'> {
}

export const ADMIN_PARK_REFERENCE_DATA_PARK_FOUNDERS_API_SERVICE_PORT = new InjectionToken<AdminParkReferenceDataParkFoundersApiServicePort>('ADMIN_PARK_REFERENCE_DATA_PARK_FOUNDERS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkFoundersApiService)
});

export interface AdminParkReferenceDataParkOperatorsApiServicePort extends Pick<ParkOperatorsApiService, 'getParkOperators'> {
}

export const ADMIN_PARK_REFERENCE_DATA_PARK_OPERATORS_API_SERVICE_PORT = new InjectionToken<AdminParkReferenceDataParkOperatorsApiServicePort>('ADMIN_PARK_REFERENCE_DATA_PARK_OPERATORS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkOperatorsApiService)
});
