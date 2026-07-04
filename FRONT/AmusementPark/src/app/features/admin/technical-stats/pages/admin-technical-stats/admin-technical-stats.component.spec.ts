import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Observable, of } from 'rxjs';

import { TechnicalStatsSettings, TechnicalStatsSnapshot } from '@app/models/admin/technical-stats/technical-stats.models';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ADMIN_TECHNICAL_STATS_DATA_PORT, AdminTechnicalStatsDataPort } from '../../state/admin-technical-stats-state-data.ports';
import { AdminTechnicalStatsComponent } from './admin-technical-stats.component';

describe('AdminTechnicalStatsComponent', () => {
  let port: jasmine.SpyObj<AdminTechnicalStatsDataPort>;

  beforeEach(async () => {
    port = jasmine.createSpyObj<AdminTechnicalStatsDataPort>('AdminTechnicalStatsDataPort', ['getStats', 'updateSettings']);
    port.getStats.and.returnValue(of(createStats(30)));
    port.updateSettings.and.returnValue(of({ persistenceRetentionDays: 15 }) as Observable<TechnicalStatsSettings>);

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminTechnicalStatsComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: ADMIN_TECHNICAL_STATS_DATA_PORT, useValue: port }
      ]
    }).compileComponents();
  });

  it('caps distribution rows rendered from large technical stats snapshots', () => {
    const fixture: ComponentFixture<AdminTechnicalStatsComponent> = TestBed.createComponent(AdminTechnicalStatsComponent);
    fixture.detectChanges();

    const cacheTab = fixture.debugElement
      .queryAll(By.css('.admin-technical-stats-tab'))
      .find((button) => button.nativeElement.textContent.includes('Cache'));
    cacheTab?.nativeElement.click();
    fixture.detectChanges();

    const statusRows = fixture.debugElement
      .queryAll(By.css('.admin-technical-stats-panel--wide .admin-technical-stats-row'))
      .filter((row) => !row.nativeElement.classList.contains('admin-technical-stats-row--robot'));
    const robotRows = fixture.debugElement.queryAll(By.css('.admin-technical-stats-row--robot'));

    expect(statusRows.length).toBe(12);
    expect(robotRows.length).toBe(12);
  });

  it('renders only the summary tab by default', () => {
    const fixture: ComponentFixture<AdminTechnicalStatsComponent> = TestBed.createComponent(AdminTechnicalStatsComponent);
    fixture.detectChanges();

    const kpis = fixture.debugElement.queryAll(By.css('.admin-technical-stats-kpi'));
    const statusRows = fixture.debugElement.queryAll(By.css('.admin-technical-stats-row'));
    const seoRows = fixture.debugElement.queryAll(By.css('.admin-technical-stats-robot-table__row'));

    expect(kpis.length).toBe(4);
    expect(statusRows.length).toBe(0);
    expect(seoRows.length).toBe(0);
  });

  it('exposes help text on help buttons for the custom tooltip', () => {
    const fixture: ComponentFixture<AdminTechnicalStatsComponent> = TestBed.createComponent(AdminTechnicalStatsComponent);
    fixture.detectChanges();

    const helpButton = fixture.debugElement.query(By.css('.admin-technical-stats-help'));

    expect(helpButton.attributes['aria-label']).toContain('Share of HTML pages');
  });

  it('filters SEO robot stats by robot family category', () => {
    const fixture: ComponentFixture<AdminTechnicalStatsComponent> = TestBed.createComponent(AdminTechnicalStatsComponent);
    fixture.detectChanges();

    const seoTab = fixture.debugElement
      .queryAll(By.css('.admin-technical-stats-tab'))
      .find((button) => button.nativeElement.textContent.includes('SEO robots'));
    seoTab?.nativeElement.click();
    fixture.detectChanges();

    const bingFilter = fixture.debugElement
      .queryAll(By.css('.admin-technical-stats-filter__button'))
      .find((button) => button.nativeElement.textContent.includes('Bing'));
    bingFilter?.nativeElement.click();
    fixture.detectChanges();

    const rows = fixture.debugElement.queryAll(By.css('.admin-technical-stats-robot-table__row'));

    expect(rows.length).toBe(1);
    expect(rows[0].nativeElement.textContent).toContain('Bingbot');
  });

  it('exposes labels on SEO robot cells for compact layouts', () => {
    const fixture: ComponentFixture<AdminTechnicalStatsComponent> = TestBed.createComponent(AdminTechnicalStatsComponent);
    fixture.detectChanges();

    const seoTab = fixture.debugElement
      .queryAll(By.css('.admin-technical-stats-tab'))
      .find((button) => button.nativeElement.textContent.includes('SEO robots'));
    seoTab?.nativeElement.click();
    fixture.detectChanges();

    const row = fixture.debugElement.query(By.css('.admin-technical-stats-robot-table__row'));
    const cells: Element[] = Array.from(row.nativeElement.children as HTMLCollectionOf<Element>);
    const labels: Array<string | null> = cells.map((cell: Element): string | null => cell.getAttribute('data-label'));

    expect(labels).toEqual([
      'Robot',
      'Group',
      'Requests',
      'Cache hit rate',
      'SEO-ready',
      'No-JS',
      'Blocked',
      '503'
    ]);
  });

  it('caps SEO robot rows rendered from large technical stats snapshots', () => {
    const fixture: ComponentFixture<AdminTechnicalStatsComponent> = TestBed.createComponent(AdminTechnicalStatsComponent);
    fixture.detectChanges();

    const seoTab = fixture.debugElement
      .queryAll(By.css('.admin-technical-stats-tab'))
      .find((button) => button.nativeElement.textContent.includes('SEO robots'));
    seoTab?.nativeElement.click();
    fixture.detectChanges();

    const rows = fixture.debugElement.queryAll(By.css('.admin-technical-stats-robot-table__row'));

    expect(rows.length).toBe(12);
  });
});

function createStats(rowCount: number): TechnicalStatsSnapshot {
  return {
    isAvailable: true,
    unavailableReason: null,
    generatedAtUtc: '2026-07-03T10:00:00.000Z',
    startedAtUtc: '2026-07-03T09:00:00.000Z',
    uptimeSeconds: 3600,
    buildVersion: '3.2.2',
    cache: {
      pageResponses: 1000,
      cacheablePageResponses: 900,
      cacheHitResponses: 800,
      hitRatePercent: 80,
      robotPageResponses: 400,
      robotCacheHitResponses: 200,
      robotHitRatePercent: 50,
      statuses: Array.from({ length: rowCount }, (_, index: number) => ({
        key: `STATUS-${index}`,
        count: rowCount - index,
        percent: 10
      })),
      robotFamilies: Array.from({ length: rowCount }, (_, index: number) => ({
        key: index === 0 ? 'Bingbot' : `Robot-${index}`,
        category: index === 0 ? 'bing' : 'other',
        count: rowCount - index,
        cacheHits: index,
        hitRatePercent: 25,
        seoReadyResponses: rowCount - index,
        seoNotReadyResponses: 0,
        seoReadyRatePercent: 100,
        noJsResponses: rowCount - index,
        blockedNotSeoReadyResponses: 0,
        htmlNotAllowedResponses: 0,
        ssrUnavailableResponses: 0
      }))
    },
    storage: {
      memoryEntries: 10,
      memoryMaxEntries: 100,
      diskEnabled: true,
      diskEntries: 20,
      diskBytes: 4096,
      diskMaxBytes: 8192,
      diskWrites: 5,
      technicalStatsPersistenceEntries: 1,
      technicalStatsPersistenceBytes: 512,
      technicalStatsPersistencePurgedBuckets: 0,
      seoDocumentEntries: 2,
      seoDocumentMaxEntries: 10,
      seoDocumentRequests: 6,
      seoDocumentHits: 4,
      seoDocumentMisses: 2,
      assetMisses: 1
    },
    seo: {
      robotNoJsHtmlEnabled: true,
      htmlResponses: 1000,
      seoReadyHtmlResponses: 990,
      seoNotReadyHtmlResponses: 10,
      seoReadyRatePercent: 99,
      robotHtmlResponses: 400,
      robotSeoReadyHtmlResponses: 390,
      robotSeoNotReadyHtmlResponses: 10,
      robotSeoReadyRatePercent: 97.5,
      robotNoJsHtmlResponses: 390,
      robotHtmlBlockedNotSeoReady: 10,
      robotHtmlNotAllowed: 0,
      robotSsrUnavailableResponses: 0,
      robotCacheOnlyMissResponses: 0,
      robotPageResponses: 400,
      robotCacheHitResponses: 200,
      robotHitRatePercent: 50,
      seoDocumentRequests: 6,
      seoDocumentHits: 4,
      seoDocumentMisses: 2,
      seoDocumentHitRatePercent: 66.7,
      queueFullRejections: 0
    },
    rendering: {
      ssrRenderEnabled: true,
      renderOnCacheMiss: false,
      renderCriticalRoutesOnCacheMiss: true,
      activeRenders: 0,
      queuedRenders: 0,
      maxConcurrency: 1,
      maxQueueEntries: 8,
      totalRenders: 10,
      averageRenderMilliseconds: 120,
      maxRenderMilliseconds: 500,
      slowRenders: 1,
      slowRenderThresholdMilliseconds: 3000,
      queueFullRejections: 0
    },
    refresh: {
      enabled: true,
      pendingRefreshes: 0,
      activeRefreshes: 0,
      deduplicatedRefreshKeys: 0,
      queuedRefreshes: 0,
      succeededRefreshes: 0,
      failedRefreshes: 0,
      maxUrls: 24,
      concurrency: 1,
      delayMilliseconds: 1500,
      timeoutSeconds: 45
    },
    invalidation: {
      requests: 0,
      allRequests: 0,
      targetedRequests: 0,
      clearedEntries: 0,
      staleEntries: 0,
      queuedRefreshes: 0,
      lastInvalidationUtc: null
    },
    config: {
      pageCacheTtlSeconds: 86400,
      stalePageCacheSeconds: 600,
      pageCacheMaxHtmlBytes: 2097152,
      pageCacheBrowserCacheControl: 'no-cache',
      csrFallbackCacheControl: 'public, max-age=60',
      seoDocumentBrowserCacheControl: 'no-cache',
      technicalStatsPersistenceEnabled: true,
      technicalStatsPersistenceRetentionDays: 15,
      technicalStatsPersistenceFlushIntervalSeconds: 60,
      technicalStatsPersistenceLastFlushUtc: null,
      technicalStatsPersistenceLastCleanupUtc: null
    }
  };
}
