import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { TechnicalStatsSnapshot } from '@app/models/admin/technical-stats/technical-stats.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AdminTechnicalStatsFacade } from './admin-technical-stats.facade';
import { ADMIN_TECHNICAL_STATS_DATA_PORT, AdminTechnicalStatsDataPort } from './admin-technical-stats-state-data.ports';

describe('AdminTechnicalStatsFacade', () => {
  let facade: AdminTechnicalStatsFacade;
  let port: jasmine.SpyObj<AdminTechnicalStatsDataPort>;

  beforeEach(() => {
    port = jasmine.createSpyObj<AdminTechnicalStatsDataPort>('AdminTechnicalStatsDataPort', ['getStats', 'updateSettings']);

    TestBed.configureTestingModule({
      providers: [
        provideCommonTestDependencies(),
        AdminTechnicalStatsFacade,
        { provide: ADMIN_TECHNICAL_STATS_DATA_PORT, useValue: port }
      ]
    });

    facade = TestBed.inject(AdminTechnicalStatsFacade);
  });

  it('loads stats and exposes KPI signals', () => {
    const stats: TechnicalStatsSnapshot = createStats();
    port.getStats.and.returnValue(of(stats));

    facade.load();

    expect(port.getStats).toHaveBeenCalled();
    expect(facade.state().kind).toBe('ready');
    expect(facade.stats()).toBe(stats);
    expect(facade.hitRatePercent()).toBe(75);
    expect(facade.robotHitRatePercent()).toBe(50);
  });

  it('keeps previous stats available when reload fails', () => {
    const stats: TechnicalStatsSnapshot = createStats();
    port.getStats.and.returnValue(of(stats));
    facade.load();

    port.getStats.and.returnValue(throwError(() => new Error('network')) as Observable<TechnicalStatsSnapshot>);
    facade.load();

    expect(facade.state().kind).toBe('error');
    expect(facade.stats()).toBe(stats);
  });

  it('updates settings and reloads stats', () => {
    const stats: TechnicalStatsSnapshot = createStats();
    port.getStats.and.returnValue(of(stats));
    port.updateSettings.and.returnValue(of({ persistenceRetentionDays: 20 }));

    facade.updateSettings({ persistenceRetentionDays: 20 });

    expect(port.updateSettings).toHaveBeenCalledWith({ persistenceRetentionDays: 20 });
    expect(port.getStats).toHaveBeenCalled();
    expect(facade.settingsSaving()).toBeFalse();
    expect(facade.settingsError()).toBeFalse();
  });

  it('flags settings update errors', () => {
    port.updateSettings.and.returnValue(throwError(() => new Error('network')));

    facade.updateSettings({ persistenceRetentionDays: 20 });

    expect(facade.settingsSaving()).toBeFalse();
    expect(facade.settingsError()).toBeTrue();
  });
});

function createStats(): TechnicalStatsSnapshot {
  return {
    generatedAtUtc: '2026-06-23T10:00:00Z',
    isAvailable: true,
    unavailableReason: null,
    startedAtUtc: '2026-06-23T09:00:00Z',
    uptimeSeconds: 3600,
    buildVersion: '2.6.18',
    cache: {
      pageResponses: 8,
      cacheablePageResponses: 8,
      cacheHitResponses: 6,
      hitRatePercent: 75,
      robotPageResponses: 4,
      robotCacheHitResponses: 2,
      robotHitRatePercent: 50,
      statuses: [{ key: 'HIT', count: 6, percent: 75 }],
      robotFamilies: [{ key: 'Googlebot', count: 4, cacheHits: 2, hitRatePercent: 50 }]
    },
    storage: {
      memoryEntries: 1,
      memoryMaxEntries: 2000,
      diskEnabled: true,
      diskEntries: 1,
      diskBytes: 1024,
      diskMaxBytes: 2048,
      diskWrites: 1,
      technicalStatsPersistenceEntries: 1,
      technicalStatsPersistenceBytes: 256,
      technicalStatsPersistencePurgedBuckets: 0,
      seoDocumentEntries: 0,
      seoDocumentMaxEntries: 128,
      seoDocumentRequests: 0,
      seoDocumentHits: 0,
      seoDocumentMisses: 0,
      assetMisses: 0
    },
    rendering: {
      ssrRenderEnabled: true,
      renderOnCacheMiss: false,
      renderCriticalRoutesOnCacheMiss: true,
      activeRenders: 0,
      queuedRenders: 0,
      maxConcurrency: 1,
      maxQueueEntries: 8,
      totalRenders: 2,
      averageRenderMilliseconds: 120,
      maxRenderMilliseconds: 180,
      slowRenders: 0,
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
