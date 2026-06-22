import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { TechnicalPage } from '@app/models/technical-pages/technical-page';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { TechnicalPagesApiService } from './technical-pages-api.service';

describe('TechnicalPagesApiService', () => {
  let service: TechnicalPagesApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(TechnicalPagesApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('gets public technical pages without a double slash in the API URL', () => {
    service.getPublicPagesPage(1, 100).subscribe((result) => {
      expect(result.items).toEqual([]);
    });

    const request = httpTestingController.expectOne((candidate) => candidate.url === `${environment.apiBaseUrl}technical-pages`);
    expect(request.request.method).toBe('GET');
    expect(request.request.url).not.toContain('/api//');
    expect(request.request.params.get('page')).toBe('1');
    expect(request.request.params.get('size')).toBe('100');
    request.flush({ data: [], pagination: { totalItems: 0, totalPages: 0, currentPage: 1, itemsPerPage: 100 } });
  });

  it('gets a technical page by slug without a double slash in the API URL', () => {
    service.getBySlug('chain-lift').subscribe((page) => {
      expect(page.slug).toBe('chain-lift');
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}technical-pages/slug/chain-lift`);
    expect(request.request.method).toBe('GET');
    expect(request.request.url).not.toContain('/api//');
    request.flush({ slug: 'chain-lift' } as TechnicalPage);
  });

  it('gets the lightweight public technical link index without a double slash in the API URL', () => {
    service.getPublicLinkIndex().subscribe((pages) => {
      expect(pages[0].slug).toBe('lap-bar');
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}technical-pages/link-index`);
    expect(request.request.method).toBe('GET');
    expect(request.request.url).not.toContain('/api//');
    request.flush([{ slug: 'lap-bar' } as TechnicalPage]);
  });

  it('upserts technical pages JSON without a double slash in the API URL', () => {
    service.upsertJson({ pages: [{ slug: 'chain-lift' } as TechnicalPage] }).subscribe((result) => {
      expect(result.createdCount).toBe(1);
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}technical-pages/upsert-json`);
    expect(request.request.method).toBe('POST');
    expect(request.request.url).not.toContain('/api//');
    expect(request.request.headers.get('Content-Type')).toBe('application/json');
    request.flush({ createdCount: 1, updatedCount: 0, pages: [{ slug: 'chain-lift' }] });
  });
});
