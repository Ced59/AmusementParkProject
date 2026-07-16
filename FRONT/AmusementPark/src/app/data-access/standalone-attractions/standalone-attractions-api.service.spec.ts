import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { StandaloneAttractionsApiService } from './standalone-attractions-api.service';

describe('StandaloneAttractionsApiService', () => {
  let service: StandaloneAttractionsApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(StandaloneAttractionsApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('loads the admin page through the admin route without adding a double slash', () => {
    service.getAdminPage(2, 20, {
      search: ' Bardonecchia ',
      countryCode: ' it ',
      type: 'RollerCoaster',
      isVisible: false,
      adminReviewStatus: 'ToReview',
      sortBy: 'updated',
      sortDirection: 'desc'
    }).subscribe((page) => {
      expect(page.items).toEqual([{ id: 'standalone-1' } as never]);
    });

    const request = httpTestingController.expectOne((candidate) => candidate.url === `${environment.apiBaseUrl}admin/standalone-attractions`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('size')).toBe('20');
    expect(request.request.params.get('search')).toBe('Bardonecchia');
    expect(request.request.params.get('countryCode')).toBe('it');
    expect(request.request.params.get('type')).toBe('RollerCoaster');
    expect(request.request.params.get('isVisible')).toBe('false');
    expect(request.request.params.get('adminReviewStatus')).toBe('ToReview');
    expect(request.request.params.get('sortBy')).toBe('updated');
    expect(request.request.params.get('sortDirection')).toBe('desc');
    request.flush({ data: [{ id: 'standalone-1' }] });
  });

  it('keeps public and admin detail routes separate', () => {
    service.getById('public id').subscribe((attraction) => {
      expect(attraction.id).toBe('public id');
    });

    const publicRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}standalone-attractions/public%20id`);
    expect(publicRequest.request.method).toBe('GET');
    publicRequest.flush({ id: 'public id' });

    service.getAdminById('admin id').subscribe((attraction) => {
      expect(attraction.id).toBe('admin id');
    });

    const adminRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/standalone-attractions/admin%20id`);
    expect(adminRequest.request.method).toBe('GET');
    adminRequest.flush({ id: 'admin id' });
  });
});
