import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { expand, map, Observable, reduce } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CountryDto } from '@app/models/countries/country-dto';
import { PagedCollectionResponse, unwrapCollection } from '../shared/api-helpers';
import { COUNTRIES_API_ENDPOINTS } from './countries-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class CountriesApiService {
  private static readonly pageSize: number = 100;

  constructor(private readonly http: HttpClient) {
  }

  getCountries(lang: string): Observable<CountryDto[]> {
    return this.getCountriesPage(lang, 1).pipe(
      expand((response: PagedCollectionResponse<CountryDto>) => {
        const currentPage: number = response.pagination?.currentPage ?? 1;
        const totalPages: number = response.pagination?.totalPages ?? 1;

        if (currentPage >= totalPages) {
          return [];
        }

        return this.getCountriesPage(lang, currentPage + 1);
      }),
      reduce((countries: CountryDto[], response: PagedCollectionResponse<CountryDto>) => [
        ...countries,
        ...unwrapCollection<CountryDto>(response)
      ], [] as CountryDto[]),
      map((countries: CountryDto[]) => this.distinctAndSortCountries(countries))
    );
  }

  private getCountriesPage(lang: string, page: number): Observable<PagedCollectionResponse<CountryDto>> {
    const url: string = `${environment.apiBaseUrl}${COUNTRIES_API_ENDPOINTS.getCountries(lang, page, CountriesApiService.pageSize)}`;
    return this.http.get<PagedCollectionResponse<CountryDto>>(url);
  }

  private distinctAndSortCountries(countries: CountryDto[]): CountryDto[] {
    const countriesByCode: Map<string, CountryDto> = new Map<string, CountryDto>();

    for (const country of countries) {
      const isoCode: string = String(country.isoCode ?? '').trim().toUpperCase();
      if (isoCode.length === 0 || countriesByCode.has(isoCode)) {
        continue;
      }

      countriesByCode.set(isoCode, {
        ...country,
        isoCode
      });
    }

    return Array.from(countriesByCode.values()).sort((left: CountryDto, right: CountryDto) =>
      left.name.localeCompare(right.name)
    );
  }
}
