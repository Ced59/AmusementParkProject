import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EventEmitter } from '@angular/core';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, Data, ParamMap, convertToParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { BehaviorSubject, Observable, of } from 'rxjs';

import { HistoryArticle, HistoryTimeline } from '@app/models/history/history.models';
import { TranslationService } from '@app/services/translation.service';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { HISTORY_DATA_PORT, HistoryDataPort } from '../state/history-data.ports';
import { HISTORY_ARTICLE_ROUTE_DATA_KEY } from '../state/history-article.resolver';
import { HistoryArticlePageComponent } from './history-article-page.component';

class FakeTranslationService {
  public readonly languageChanged: EventEmitter<string> = new EventEmitter<string>();

  getCurrentLang(): string {
    return 'fr';
  }
}

class FakeHistoryDataPort implements HistoryDataPort {
  public articleCallCount: number = 0;

  getParkTimeline(_parkId: string, _includeParkItems?: boolean, _parkItemIds?: readonly string[], _options?: AnonymousHttpOptions): Observable<HistoryTimeline> {
    return of({} as HistoryTimeline);
  }

  getParkItemTimeline(_parkItemId: string, _options?: AnonymousHttpOptions): Observable<HistoryTimeline> {
    return of({} as HistoryTimeline);
  }

  getArticle(_eventId: string, _options?: AnonymousHttpOptions): Observable<HistoryArticle> {
    this.articleCallCount += 1;
    return of(createHistoryArticle('Fallback Article'));
  }
}

describe('HistoryArticlePageComponent', () => {
  let fixture: ComponentFixture<HistoryArticlePageComponent>;
  let historyDataPort: FakeHistoryDataPort;
  let seoService: jasmine.SpyObj<SeoService>;
  let routeParamMap: BehaviorSubject<ParamMap>;
  let routeData: BehaviorSubject<Data>;

  beforeEach(async () => {
    historyDataPort = new FakeHistoryDataPort();
    seoService = jasmine.createSpyObj<SeoService>('SeoService', ['applyHistoryArticleSeo']);
    routeParamMap = new BehaviorSubject<ParamMap>(convertToParamMap({ eventId: 'event-1' }));
    routeData = new BehaviorSubject<Data>({
      [HISTORY_ARTICLE_ROUTE_DATA_KEY]: createHistoryArticle('Arrêt de Nitro')
    });

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, HistoryArticlePageComponent],
      providers: [
        ...provideCommonTestDependencies(),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ eventId: 'event-1' }) },
            parent: {
              snapshot: { paramMap: convertToParamMap({ lang: 'fr' }) },
              parent: null
            },
            paramMap: routeParamMap.asObservable(),
            data: routeData.asObservable()
          }
        },
        { provide: HISTORY_DATA_PORT, useValue: historyDataPort },
        { provide: SeoService, useValue: seoService },
        { provide: SsrHttpStatusService, useValue: jasmine.createSpyObj<SsrHttpStatusService>('SsrHttpStatusService', ['setNotFound']) },
        { provide: TranslationService, useClass: FakeTranslationService }
      ]
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      shareSocial: {
        historyArticle: {
          title: 'Partager cet article',
          description: 'Envoie cet article.',
          text: 'Lis {{title}} sur AmusementPark.'
        },
        actions: {
          share: 'Partager',
          copy: 'Copier',
          copied: 'Copié',
          linkedin: 'LinkedIn',
          facebook: 'Facebook',
          x: 'X',
          reddit: 'Reddit',
          more: 'Plus',
          less: 'Moins',
          email: 'Email',
          whatsapp: 'WhatsApp',
          telegram: 'Telegram',
          qrCode: 'QR code'
        },
        qr: {
          title: 'Partager via QR code',
          loading: 'Génération du QR code',
          alt: 'QR code pour {{title}}'
        }
      }
    });
    translateService.use('fr');

    fixture = TestBed.createComponent(HistoryArticlePageComponent);
    fixture.detectChanges();
  });

  it('renders the resolved article context without a late API load', () => {
    const textContent: string = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(textContent).toContain('Arrêt de Nitro');
    expect(textContent).toContain('Le Nitro');
    expect(textContent).toContain('Dennlys Parc');
    expect(historyDataPort.articleCallCount).toBe(0);
  });

  it('uses the resolved article context for sharing and SEO', () => {
    const sharePanel: PublicSharePanelComponent = fixture.debugElement
      .query(By.directive(PublicSharePanelComponent))
      .componentInstance as PublicSharePanelComponent;

    expect(sharePanel.targetTitle).toBe('Arrêt de Nitro - Le Nitro - Dennlys Parc');
    expect(seoService.applyHistoryArticleSeo).toHaveBeenCalledWith(
      jasmine.objectContaining({
        title: 'Arrêt de Nitro',
        parkItem: jasmine.objectContaining({ name: 'Le Nitro' }),
        contextPark: jasmine.objectContaining({ name: 'Dennlys Parc' })
      }),
      'fr',
      jasmine.any(String),
      jasmine.any(String)
    );
  });
});

function createHistoryArticle(title: string): HistoryArticle {
  return {
    event: {
      id: 'event-1',
      key: 'event-1',
      entityType: 'ParkItem',
      ownerId: 'item-1',
      parkId: 'park-1',
      parkItemId: 'item-1',
      contextParkId: 'park-1',
      year: 2026,
      month: 7,
      day: 4,
      datePrecision: 'Day',
      eventType: 'Incident',
      isMajor: true,
      isVisible: true,
      slug: 'nitro-incident',
      titles: [],
      summaries: [],
      mainImageId: 'image-1',
      previousName: null,
      newName: null,
      previousLogoImageId: null,
      newLogoImageId: null,
      previousOperatorId: null,
      newOperatorId: null,
      locationLabel: null,
      relatedParkIds: [],
      relatedParkItemIds: [],
      sources: [],
      article: {
        slug: 'nitro-incident',
        titles: [{ languageCode: 'fr', value: title }],
        subtitles: [],
        summaries: [{ languageCode: 'fr', value: 'Un résumé public de l’incident.' }],
        mainImageId: 'image-1',
        blocks: [
          {
            id: 'paragraph-1',
            type: 'Paragraph',
            sortOrder: 1,
            headingLevel: null,
            texts: [{ languageCode: 'fr', value: 'Le contenu de l’article est rendu dès le SSR.' }],
            imageId: null,
            imageIds: [],
            captions: []
          }
        ],
        sources: [],
        isPublished: true
      },
      createdAtUtc: '2026-07-04T00:00:00Z',
      updatedAtUtc: '2026-07-04T00:00:00Z'
    },
    contextPark: {
      id: 'park-1',
      name: 'Dennlys Parc',
      countryCode: 'FR',
      latitude: 50.58,
      longitude: 2.17,
      isVisible: true
    },
    park: null,
    parkItem: {
      id: 'item-1',
      parkId: 'park-1',
      name: 'Le Nitro',
      category: 'Attraction',
      type: 'RollerCoaster',
      latitude: null,
      longitude: null,
      isVisible: true,
      mainImageId: null
    },
    mainImage: null
  };
}
