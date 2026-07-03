import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AdminTechnicalStatsApiService } from './admin-technical-stats-api.service';

describe('AdminTechnicalStatsApiService', () => {
  let service: AdminTechnicalStatsApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(AdminTechnicalStatsApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('loads technical stats from the admin endpoint', () => {
    service.getStats().subscribe((response) => {
      expect(response.buildVersion).toBe('2.6.18');
      expect(response.cache.hitRatePercent).toBe(80);
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/technical-stats`);
    expect(request.request.method).toBe('GET');
    request.flush({
      generatedAtUtc: '2026-06-23T10:00:00Z',
      isAvailable: true,
      unavailableReason: null,
      startedAtUtc: '2026-06-23T09:00:00Z',
      uptimeSeconds: 3600,
      buildVersion: '2.6.18',
      cache: {
        pageResponses: 10,
        cacheablePageResponses: 10,
        cacheHitResponses: 8,
        hitRatePercent: 80,
        robotPageResponses: 5,
        robotCacheHitResponses: 4,
        robotHitRatePercent: 80,
        statuses: [],
        robotFamilies: []
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
      seo: {
        robotNoJsHtmlEnabled: true,
        htmlResponses: 10,
        seoReadyHtmlResponses: 10,
        seoNotReadyHtmlResponses: 0,
        seoReadyRatePercent: 100,
        robotHtmlResponses: 5,
        robotSeoReadyHtmlResponses: 5,
        robotSeoNotReadyHtmlResponses: 0,
        robotSeoReadyRatePercent: 100,
        robotNoJsHtmlResponses: 5,
        robotHtmlBlockedNotSeoReady: 0,
        robotHtmlNotAllowed: 0,
        robotSsrUnavailableResponses: 0,
        robotPageResponses: 5,
        robotCacheHitResponses: 4,
        robotHitRatePercent: 80,
        seoDocumentRequests: 0,
        seoDocumentHits: 0,
        seoDocumentMisses: 0,
        seoDocumentHitRatePercent: 0,
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
    });
  });

  it('updates technical stats settings', () => {
    service.updateSettings({ persistenceRetentionDays: 20 }).subscribe((response) => {
      expect(response.persistenceRetentionDays).toBe(20);
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/technical-stats/settings`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ persistenceRetentionDays: 20 });
    request.flush({ persistenceRetentionDays: 20 });
  });
});
