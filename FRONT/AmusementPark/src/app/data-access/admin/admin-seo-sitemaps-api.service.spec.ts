import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AdminSeoSitemapsApiService } from './admin-seo-sitemaps-api.service';
import { GenerateSeoSitemapRequest, UpdateSeoSitemapSettingsRequest } from '@app/models/admin/seo/seo-sitemap.models';

describe('AdminSeoSitemapsApiService', () => {
  let service: AdminSeoSitemapsApiService;
  let httpTestingController: HttpTestingController;
  const baseUrl: string = `${environment.apiBaseUrl}admin/seo/sitemaps`;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(AdminSeoSitemapsApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('gets overview and settings', () => {
    service.getOverview().subscribe();
    service.getSettings().subscribe();

    const overviewRequest = httpTestingController.expectOne(`${baseUrl}/overview`);
    expect(overviewRequest.request.method).toBe('GET');
    overviewRequest.flush({});

    const settingsRequest = httpTestingController.expectOne(`${baseUrl}/settings`);
    expect(settingsRequest.request.method).toBe('GET');
    settingsRequest.flush({});
  });

  it('updates settings and generates sitemaps with request bodies', () => {
    const settingsRequest: UpdateSeoSitemapSettingsRequest = {
      isIndexNowEnabled: true,
      submitToIndexNowAfterManualGeneration: true,
      submitToIndexNowAfterAutomaticGeneration: false,
      indexNowKey: 'key',
      indexNowKeyLocation: '/key.txt',
      indexNowEndpoints: ['https://api.indexnow.org/indexnow']
    };
    const generationRequest: GenerateSeoSitemapRequest = { submitToIndexNow: true };

    service.updateSettings(settingsRequest).subscribe();
    service.generate(generationRequest).subscribe();

    const updateRequest = httpTestingController.expectOne(`${baseUrl}/settings`);
    expect(updateRequest.request.method).toBe('PUT');
    expect(updateRequest.request.body).toEqual(settingsRequest);
    updateRequest.flush({});

    const generateRequest = httpTestingController.expectOne(`${baseUrl}/generate`);
    expect(generateRequest.request.method).toBe('POST');
    expect(generateRequest.request.body).toEqual(generationRequest);
    generateRequest.flush({});
  });

  it('gets history with page and size params', () => {
    service.getHistory(3, 50).subscribe();

    const request = httpTestingController.expectOne((candidate) => candidate.url === `${baseUrl}/history`);
    expect(request.request.params.get('page')).toBe('3');
    expect(request.request.params.get('size')).toBe('50');
    request.flush({ data: [], pagination: { totalItems: 0, totalPages: 0, currentPage: 3, itemsPerPage: 50 } });
  });
});
