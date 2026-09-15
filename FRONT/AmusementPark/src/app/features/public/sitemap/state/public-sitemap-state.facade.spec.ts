import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';

import { PublicHtmlSitemapNode } from '@app/models/seo/public-html-sitemap-node';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { PUBLIC_SITEMAP_DATA_PORT } from './public-sitemap-data.ports';
import { PublicSitemapStateFacade } from './public-sitemap-state.facade';

function node(id: string, label: string, hasChildren: boolean = true): PublicHtmlSitemapNode {
  return { id, label, hasChildren, relativeUrl: `/fr/${id.replace(':', '/')}` };
}

const branches: Record<string, PublicHtmlSitemapNode[]> = {
  root: [node('parks', 'Parcs'), node('references', 'Références'), node('snapshot-sections', 'Toutes les pages')],
  parks: [node('park:park-1', 'Parc Démo')],
  'park:park-1': [node('park-items:park-1', 'Attractions'), node('park-history:park-1', 'Histoire')],
  'park-items:park-1': [node('park-item:item-1', 'Montagne russe')],
  'park-history:park-1': [node('park-article:article-1', 'Une nouvelle époque', false)],
  'snapshot-sections': [node('sitemap-section:history-fr', 'Histoire')],
  'sitemap-section:history-fr': [{ id: 'history-page', label: 'Tour panoramique · Histoire · Page 2', relativeUrl: '/fr/attraction/tower/tour-panoramique/history/page/2', hasChildren: false }]
};

describe('PublicSitemapStateFacade', () => {
  const dataPort = { getNodes: vi.fn() };
  const status = { setNotFound: vi.fn(), setStatus: vi.fn() };
  let facade: PublicSitemapStateFacade;

  beforeEach(() => {
    vi.clearAllMocks();
    dataPort.getNodes.mockImplementation((_language: string, parent: string | null) => of(branches[parent ?? 'root'] ?? []));
    TestBed.configureTestingModule({
      providers: [PublicSitemapStateFacade,
        { provide: PUBLIC_SITEMAP_DATA_PORT, useValue: dataPort },
        { provide: SsrHttpStatusService, useValue: status }]
    });
    facade = TestBed.inject(PublicSitemapStateFacade);
  });

  it('exposes root branch links without loading descendants', () => {
    facade.loadPage('fr', { nodeIds: [], page: 1, isValid: true });

    expect(dataPort.getNodes.mock.calls).toEqual([['fr', null, false]]);
    expect(facade.nodes()[0].branchQueryParams).toEqual({ node: 'parks' });
    expect(facade.loading()).toBe(false);
    expect(facade.resolvedPage()).toEqual({ language: 'fr', location: { nodeIds: [], page: 1, isValid: true }, breadcrumbLabels: [] });
  });

  it('resolves only the selected hierarchy with contextual breadcrumb labels', () => {
    facade.loadPage('fr', { nodeIds: ['parks', 'park:park-1', 'park-items:park-1'], page: 1, isValid: true });

    expect(dataPort.getNodes.mock.calls).toEqual([
      ['fr', null, false], ['fr', 'parks', false], ['fr', 'park:park-1', false], ['fr', 'park-items:park-1', false]
    ]);
    expect(facade.breadcrumbs().map(breadcrumb => breadcrumb.label)).toEqual(['Parcs', 'Parc Démo', 'Attractions']);
    expect(facade.nodes()[0].label).toBe('Montagne russe');
    expect(facade.nodes()[0].branchQueryParams).toEqual({ node: 'parks/park:park-1/park-items:park-1/park-item:item-1' });
    expect(facade.resolvedPage()?.breadcrumbLabels).toEqual(['Parcs', 'Parc Démo', 'Attractions']);
  });

  it('keeps article links as direct destinations', () => {
    facade.loadPage('fr', { nodeIds: ['parks', 'park:park-1', 'park-history:park-1'], page: 1, isValid: true });

    expect(facade.nodes()[0].relativeUrl).toBe('/fr/park-article/article-1');
    expect(facade.nodes()[0].branchQueryParams).toBeNull();
  });

  it('exposes complementary snapshot sections through the same bounded hierarchy', () => {
    facade.loadPage('fr', { nodeIds: ['snapshot-sections', 'sitemap-section:history-fr'], page: 1, isValid: true });

    expect(dataPort.getNodes.mock.calls).toEqual([
      ['fr', null, false], ['fr', 'snapshot-sections', false], ['fr', 'sitemap-section:history-fr', false]
    ]);
    expect(facade.nodes()[0].relativeUrl).toBe('/fr/attraction/tower/tour-panoramique/history/page/2');
    expect(facade.nodes()[0].branchQueryParams).toBeNull();
  });

  it('paginates a sibling collection without dropping entries or requesting the whole snapshot', () => {
    const parks: PublicHtmlSitemapNode[] = Array.from({ length: 105 }, (_, index) => node(`park:park-${index}`, `Parc ${index}`));
    dataPort.getNodes.mockImplementation((_language: string, parent: string | null) => of(parent === 'parks' ? parks : branches['root']));

    facade.loadPage('fr', { nodeIds: ['parks'], page: 1, isValid: true });
    const firstPage: string[] = facade.nodes().map(park => park.id);
    expect(firstPage).toHaveLength(100);
    expect(facade.pageCount()).toBe(2);
    facade.loadPage('fr', { nodeIds: ['parks'], page: 2, isValid: true });

    expect([...firstPage, ...facade.nodes().map(park => park.id)]).toEqual(parks.map(park => park.id));
    expect(dataPort.getNodes.mock.calls.every(call => call[2] === false)).toBe(true);
  });

  it('rejects malformed locations before requesting data', () => {
    facade.loadPage('fr', { nodeIds: ['../admin'], page: 1, isValid: false });

    expect(dataPort.getNodes).not.toHaveBeenCalled();
    expect(status.setNotFound).toHaveBeenCalled();
    expect(facade.loading()).toBe(false);
  });

  it('does not query an unknown or misplaced node', () => {
    facade.loadPage('fr', { nodeIds: ['parks', 'park:unknown'], page: 1, isValid: true });

    expect(dataPort.getNodes.mock.calls).toEqual([['fr', null, false], ['fr', 'parks', false]]);
    expect(status.setNotFound).toHaveBeenCalled();
    expect(facade.nodes()).toEqual([]);
  });

  it('returns not found for a page beyond the sibling collection', () => {
    facade.loadPage('fr', { nodeIds: ['parks'], page: 2, isValid: true });

    expect(status.setNotFound).toHaveBeenCalled();
    expect(facade.nodes()).toEqual([]);
  });

  it('cancels obsolete branch requests when the location or language changes', () => {
    const oldResponse = new Subject<PublicHtmlSitemapNode[]>();
    dataPort.getNodes.mockImplementation((language: string) => language === 'fr' ? oldResponse : of([node('parks', 'Parks')]));

    facade.loadPage('fr', { nodeIds: [], page: 1, isValid: true });
    facade.loadPage('en', { nodeIds: [], page: 1, isValid: true });
    oldResponse.next(branches['root']);

    expect(oldResponse.observed).toBe(false);
    expect(facade.nodes().map(value => value.label)).toEqual(['Parks']);
    expect(facade.resolvedPage()?.language).toBe('en');
  });

  it('marks unavailable API responses as temporary server failures', () => {
    dataPort.getNodes.mockReturnValue(throwError(() => new Error('network')));
    facade.loadPage('fr', { nodeIds: [], page: 1, isValid: true });

    expect(status.setStatus).toHaveBeenCalledWith(503);
    expect(facade.errorKey()).toBe('sitemapPage.error');
    expect(facade.loading()).toBe(false);
    expect(facade.resolvedPage()).toBeNull();
  });

  it('invalidates the previous resolved SEO context before another request finishes', () => {
    facade.loadPage('fr', { nodeIds: [], page: 1, isValid: true });
    expect(facade.resolvedPage()).not.toBeNull();
    dataPort.getNodes.mockReturnValue(new Subject<PublicHtmlSitemapNode[]>());
    facade.loadPage('fr', { nodeIds: ['parks'], page: 1, isValid: true });
    expect(facade.resolvedPage()).toBeNull();
  });

  it('does not expose a resolved SEO context for empty or out-of-bounds content', () => {
    dataPort.getNodes.mockReturnValue(of([]));
    facade.loadPage('fr', { nodeIds: [], page: 1, isValid: true });
    expect(facade.resolvedPage()).toBeNull();
    facade.loadPage('fr', { nodeIds: [], page: 2, isValid: true });
    expect(facade.resolvedPage()).toBeNull();
    expect(status.setNotFound).toHaveBeenCalled();
  });

  it('clears a resolved SEO context if a data stream subsequently fails', () => {
    const response = new Subject<PublicHtmlSitemapNode[]>();
    dataPort.getNodes.mockReturnValue(response);
    facade.loadPage('fr', { nodeIds: [], page: 1, isValid: true });
    response.next(branches['root']);
    expect(facade.resolvedPage()).not.toBeNull();
    response.error(new Error('network'));
    expect(facade.resolvedPage()).toBeNull();
    expect(status.setStatus).toHaveBeenCalledWith(503);
  });
});
