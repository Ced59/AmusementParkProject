import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CountryDto } from '../../models/countries/country-dto';
import { PagedCollectionResponse, unwrapCollection } from '../shared/api-helpers';
import { COUNTRIES_API_ENDPOINTS } from './countries-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class CountriesApiService {
  constructor(private readonly http: HttpClient) {
  }

  getCountries(lang: string): Observable<CountryDto[]> {
    const url: string = `${environment.apiBaseUrl}${COUNTRIES_API_ENDPOINTS.getCountries(lang)}`;
    return this.http.get<CountryDto[] | PagedCollectionResponse<CountryDto>>(url).pipe(
      map((response: CountryDto[] | PagedCollectionResponse<CountryDto>) => unwrapCollection<CountryDto>(response))
    );
  }
}
