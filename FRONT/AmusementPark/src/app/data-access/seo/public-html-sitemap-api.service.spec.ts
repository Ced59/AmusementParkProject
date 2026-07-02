import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { PublicHtmlSitemapNode } from '@app/models/seo/public-html-sitemap-node';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { PublicHtmlSitemapApiService } from './public-html-sitemap-api.service';

describe('PublicHtmlSitemapApiService', () => {
  let service: PublicHtmlSitemapApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(PublicHtmlSitemapApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('gets current language sitemap nodes without a double slash in the API URL', () => {
    service.getNodes('fr', 'parks').subscribe((nodes: PublicHtmlSitemapNode[]) => {
      expect(nodes[0].id).toBe('park:park-1');
    });

    const request = httpTestingController.expectOne((candidate) => candidate.url === `${environment.apiBaseUrl}seo/html-sitemap/nodes`);
    expect(request.request.method).toBe('GET');
    expect(request.request.url).not.toContain('/api//');
    expect(request.request.params.get('language')).toBe('fr');
    expect(request.request.params.get('parentNodeId')).toBe('parks');
    expect(request.request.params.has('includeDescendants')).toBeFalse();
    request.flush([{ id: 'park:park-1', label: 'Parc Demo', relativeUrl: '/fr/park/park-1/parc-demo', hasChildren: true }]);
  });

  it('can request the complete sitemap tree', () => {
    service.getNodes('fr', null, true).subscribe((nodes: PublicHtmlSitemapNode[]) => {
      expect(nodes[0].id).toBe('parks');
    });

    const request = httpTestingController.expectOne((candidate) => candidate.url === `${environment.apiBaseUrl}seo/html-sitemap/nodes`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('language')).toBe('fr');
    expect(request.request.params.has('parentNodeId')).toBeFalse();
    expect(request.request.params.get('includeDescendants')).toBe('true');
    request.flush([{ id: 'parks', label: 'Parcs', relativeUrl: '/fr/parks', hasChildren: true, children: [] }]);
  });
});
