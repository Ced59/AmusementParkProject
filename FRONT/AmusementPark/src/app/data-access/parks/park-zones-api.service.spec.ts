import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ParkZone } from '@app/models/parks/park-zone';
import { environment } from '../../../environments/environment';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ParkZonesApiService } from './park-zones-api.service';

describe('ParkZonesApiService', () => {
  let service: ParkZonesApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(ParkZonesApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('unwraps park zones returned in a paged response', () => {
    let result: ParkZone[] = [];
    service.getParkZonesByParkId('park-1').subscribe((zones: ParkZone[]) => {
      result = zones;
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}park-zones/park/park-1`);
    expect(request.request.method).toBe('GET');
    request.flush({ data: [{ id: 'zone-1', parkId: 'park-1', name: 'Zone' }] });

    expect(result).toEqual([{ id: 'zone-1', parkId: 'park-1', name: 'Zone' } as ParkZone]);
  });

  it('sends create, update and delete requests with expected methods and bodies', () => {
    const zone: ParkZone = { id: 'zone-1', parkId: 'park-1', name: 'Zone' };

    service.createParkZone(zone).subscribe();
    service.updateParkZone('zone-1', zone).subscribe();
    service.deleteParkZone('zone-1').subscribe();

    const createRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}park-zones`);
    expect(createRequest.request.method).toBe('POST');
    expect(createRequest.request.body).toBe(zone);
    createRequest.flush(zone);

    const updateRequest = httpTestingController.expectOne((request) => request.url === `${environment.apiBaseUrl}park-zones/zone-1` && request.method === 'PUT');
    expect(updateRequest.request.method).toBe('PUT');
    expect(updateRequest.request.body).toBe(zone);
    updateRequest.flush(zone);

    const deleteRequest = httpTestingController.expectOne((request) => request.url === `${environment.apiBaseUrl}park-zones/zone-1` && request.method === 'DELETE');
    expect(deleteRequest.request.method).toBe('DELETE');
    deleteRequest.flush(true);
  });
});
