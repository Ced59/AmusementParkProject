import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ContactApiService } from './contact-api.service';

describe('ContactApiService', () => {
  let service: ContactApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(ContactApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('submits a public grievance to the contact endpoint', () => {
    const requestBody = {
      message: 'A public suggestion.',
      website: null,
      languageCode: 'en'
    };

    service.submitGrievance(requestBody).subscribe((response) => {
      expect(response.accepted).toBeTrue();
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}contact/grievances`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(requestBody);
    request.flush({ accepted: true, submittedAtUtc: '2026-06-17T00:00:00Z' });
  });

  it('searches admin grievances with trimmed optional query params', () => {
    service.searchAdminGrievances({
      page: 2,
      size: 25,
      search: ' queue ',
      ipAddress: ' 127.0.0.1 ',
      languageCode: ' fr '
    }).subscribe((response) => {
      expect(response.data).toEqual([{ id: 'grievance-1' } as never]);
    });

    const request = httpTestingController.expectOne((candidate) => candidate.url === `${environment.apiBaseUrl}admin/contact/grievances`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('size')).toBe('25');
    expect(request.request.params.get('search')).toBe('queue');
    expect(request.request.params.get('ipAddress')).toBe('127.0.0.1');
    expect(request.request.params.get('languageCode')).toBe('fr');
    request.flush({
      data: [{ id: 'grievance-1' }],
      pagination: { totalItems: 1, totalPages: 1, currentPage: 2, itemsPerPage: 25 }
    });
  });
});
