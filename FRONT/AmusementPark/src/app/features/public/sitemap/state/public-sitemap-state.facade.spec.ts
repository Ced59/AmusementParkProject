import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { PublicHtmlSitemapNode } from '@app/models/seo/public-html-sitemap-node';
import { PUBLIC_SITEMAP_DATA_PORT, PublicSitemapDataPort } from './public-sitemap-data.ports';
import { PublicSitemapStateFacade } from './public-sitemap-state.facade';

describe('PublicSitemapStateFacade', () => {
  let dataPort: jasmine.SpyObj<PublicSitemapDataPort>;
  let facade: PublicSitemapStateFacade;

  beforeEach(() => {
    dataPort = jasmine.createSpyObj<PublicSitemapDataPort>('PublicSitemapDataPort', ['getNodes']);

    TestBed.configureTestingModule({
      providers: [
        PublicSitemapStateFacade,
        { provide: PUBLIC_SITEMAP_DATA_PORT, useValue: dataPort }
      ]
    });

    facade = TestBed.inject(PublicSitemapStateFacade);
  });

  it('loads root nodes and lazy loads a branch only once', () => {
    const parksNode: PublicHtmlSitemapNode = {
      id: 'parks',
      label: 'Parcs',
      relativeUrl: '/fr/parks',
      hasChildren: true
    };
    dataPort.getNodes.and.callFake((language: string, parentNodeId: string | null) => {
      if (parentNodeId === 'parks') {
        return of([
          {
            id: 'park:park-1',
            label: 'Parc Demo',
            relativeUrl: `/${language}/park/park-1/parc-demo`,
            hasChildren: true
          }
        ]);
      }

      return of([parksNode]);
    });

    facade.loadRoot('fr');
    facade.toggleNode(parksNode);
    facade.toggleNode(parksNode);
    facade.toggleNode(parksNode);

    expect(dataPort.getNodes.calls.allArgs()).toEqual([
      ['fr', null, false],
      ['fr', 'parks', false]
    ]);
    expect(facade.rootNodes()).toEqual([parksNode]);
    expect(facade.childrenFor('parks')[0].relativeUrl).toBe('/fr/park/park-1/parc-demo');
    expect(facade.isExpanded('parks')).toBeTrue();
  });

  it('loads embedded descendants in the background without blocking root display', () => {
    const parkNode: PublicHtmlSitemapNode = {
      id: 'park:park-1',
      label: 'Parc Demo',
      relativeUrl: '/fr/park/park-1/parc-demo',
      hasChildren: false
    };
    const rootParksNode: PublicHtmlSitemapNode = {
      id: 'parks',
      label: 'Parcs',
      relativeUrl: '/fr/parks',
      hasChildren: true
    };
    const parksNode: PublicHtmlSitemapNode = {
      id: 'sitemap-section:parks',
      label: 'Parcs',
      relativeUrl: null,
      hasChildren: true,
      children: [parkNode]
    };
    dataPort.getNodes.and.callFake((_: string, __: string | null, includeDescendants?: boolean) => {
      return of(includeDescendants ? [parksNode] : [rootParksNode]);
    });

    facade.loadRoot('fr', true);

    expect(dataPort.getNodes.calls.allArgs()).toEqual([
      ['fr', null, false],
      ['fr', null, true]
    ]);
    expect(facade.loading()).toBeFalse();
    expect(facade.rootNodes()).toEqual([rootParksNode, parksNode]);
    expect(facade.childrenFor('sitemap-section:parks')).toEqual([parkNode]);
    expect(facade.isExpanded('sitemap-section:parks')).toBeTrue();
  });

  it('exposes an error key when root loading fails', () => {
    dataPort.getNodes.and.returnValue(throwError(() => new Error('network')));

    facade.loadRoot('fr');

    expect(facade.loading()).toBeFalse();
    expect(facade.rootNodes()).toEqual([]);
    expect(facade.errorKey()).toBe('sitemapPage.error');
  });
});
