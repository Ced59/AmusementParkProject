import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { SKIP_AUTHORIZATION_HEADER } from '@core/http/auth/auth-request-policy';
import { NearbyDestinationsApiService } from './nearby-destinations-api.service';

describe('NearbyDestinationsApiService compact transport', () => {
  it('also accepts legacy responses during deployment without a retry or refetch', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const http = TestBed.inject(HttpTestingController);
    const legacy = [{ kind: 'Park', park: { id: 'park', name: 'Parc', latitude: 48, longitude: 2 }, standaloneAttraction: null, distanceKilometers: 1.6 }];
    TestBed.inject(NearbyDestinationsApiService).getNearby('Park', 'source', 4, anonymousHttpOptions())
      .subscribe(items => expect(items).toEqual(legacy));
    http.expectOne(request => request.url.endsWith('/nearby-destinations/Park/source')).flush(legacy);
    http.verify();
  });

  it('requests only card data and restores both kinds without losing localized values or images', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const http = TestBed.inject(HttpTestingController);
    const service = TestBed.inject(NearbyDestinationsApiService);
    service.getNearby('Park', 'source', 4, anonymousHttpOptions()).subscribe(items => {
      expect(items[0].park?.name).toBe('Parc');
      expect(items[0].park?.currentLogoImageId).toBe('logo');
      expect(items[0].park?.status).toBe('TemporarilyClosed');
      expect(items[0].shortDescriptions).toEqual([{ languageCode: 'fr', value: 'Bref' }, { languageCode: 'en', value: 'Short' }]);
      expect(items[1].standaloneAttraction?.attractionDetails?.status).toBe('Operating');
      expect(items[1].photoImageId).toBe('photo');
      expect(items.map(item => item.distanceKilometers)).toEqual([1.6, 3.2]);
    });
    const request = http.expectOne(r => r.url.endsWith('/nearby-destinations/Park/source'));
    expect(request.request.params.get('cardView')).toBe('true');
    expect(request.request.params.get('groupByKind')).toBe('true');
    expect(request.request.params.get('limit')).toBe('4');
    expect(request.request.context.get(SKIP_AUTHORIZATION_HEADER)).toBe(true);
    const common = { countryCode: 'FR', city: 'Paris', latitude: 48, longitude: 2, shortDescriptions: [{ languageCode: 'fr', value: 'Bref' }, { languageCode: 'en', value: 'Short' }] };
    request.flush([
      { ...common, kind: 'Park', id: 'park', name: 'Parc', logoImageId: 'logo', parkStatus: 'TemporarilyClosed', distanceKilometers: 1.6 },
      { ...common, kind: 'StandaloneAttraction', id: 'ride', name: 'Ride', photoImageId: 'photo', attractionStatus: 'Operating', distanceKilometers: 3.2 }
    ]);
    http.verify();
  });
});
