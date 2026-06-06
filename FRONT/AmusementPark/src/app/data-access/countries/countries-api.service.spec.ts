import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { CountryDto } from '@app/models/countries/country-dto';
import { environment } from '../../../environments/environment';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { CountriesApiService } from './countries-api.service';

describe('CountriesApiService', () => {
  let service: CountriesApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(CountriesApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('loads all pages, deduplicates by ISO code and sorts by localized name', () => {
    let result: CountryDto[] = [];
    service.getCountries('fr').subscribe((countries: CountryDto[]) => {
      result = countries;
    });

    httpTestingController.expectOne(`${environment.apiBaseUrl}countries?lang=fr&page=1&size=100`).flush({
      data: [
        { isoCode: 'be', name: 'Belgique' },
        { isoCode: 'FR', name: 'France' }
      ],
      pagination: { currentPage: 1, totalPages: 2, totalItems: 3, itemsPerPage: 2 }
    });
    httpTestingController.expectOne(`${environment.apiBaseUrl}countries?lang=fr&page=2&size=100`).flush({
      data: [
        { isoCode: 'BE', name: 'Belgique duplicate' },
        { isoCode: 'DE', name: 'Allemagne' }
      ],
      pagination: { currentPage: 2, totalPages: 2, totalItems: 3, itemsPerPage: 2 }
    });

    expect(result).toEqual([
      { isoCode: 'DE', name: 'Allemagne' },
      { isoCode: 'BE', name: 'Belgique' },
      { isoCode: 'FR', name: 'France' }
    ]);
  });

  it('handles APIs returning array-like data without pagination as a single page', () => {
    let result: CountryDto[] = [];
    service.getCountries('en').subscribe((countries: CountryDto[]) => {
      result = countries;
    });

    httpTestingController.expectOne(`${environment.apiBaseUrl}countries?lang=en&page=1&size=100`).flush({
      data: [{ isoCode: 'US', name: 'United States' }]
    });

    expect(result).toEqual([{ isoCode: 'US', name: 'United States' }]);
  });
});
