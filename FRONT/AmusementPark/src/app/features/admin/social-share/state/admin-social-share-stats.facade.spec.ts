import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import {
  SocialShareStatsQuery,
  SocialShareStatsResult
} from '@app/models/social-share/social-share.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AdminSocialShareStatsFacade } from './admin-social-share-stats.facade';
import {
  ADMIN_SOCIAL_SHARE_STATS_DATA_PORT,
  AdminSocialShareStatsDataPort
} from './admin-social-share-stats-state-data.ports';

describe('AdminSocialShareStatsFacade', () => {
  let facade: AdminSocialShareStatsFacade;
  let port: jasmine.SpyObj<AdminSocialShareStatsDataPort>;

  const stats: SocialShareStatsResult = {
    fromUtc: '2026-06-01T00:00:00Z',
    toUtc: '2026-06-17T23:59:59Z',
    totalEvents: 4,
    anonymousEvents: 3,
    authenticatedEvents: 1,
    daily: [{ date: '2026-06-17', count: 4 }],
    channels: [{ key: 'Copy', count: 2 }],
    targetTypes: [{ key: 'Park', count: 4 }],
    visitorKinds: [{ key: 'Anonymous', count: 3 }],
    topTargets: [{
      targetType: 'Park',
      targetId: 'park-1',
      targetTitle: 'Test Park',
      url: 'https://example.test/fr/park/park-1/test-park',
      count: 4
    }]
  };

  beforeEach(() => {
    port = jasmine.createSpyObj<AdminSocialShareStatsDataPort>('AdminSocialShareStatsDataPort', ['getStats']);

    TestBed.configureTestingModule({
      providers: [
        provideCommonTestDependencies(),
        AdminSocialShareStatsFacade,
        { provide: ADMIN_SOCIAL_SHARE_STATS_DATA_PORT, useValue: port }
      ]
    });

    facade = TestBed.inject(AdminSocialShareStatsFacade);
  });

  it('loads stats and exposes dashboard signals', () => {
    const query: SocialShareStatsQuery = {
      fromUtc: '2026-06-01T00:00:00Z',
      toUtc: '2026-06-17T23:59:59Z'
    };
    port.getStats.and.returnValue(of(stats));

    facade.load(query);

    expect(port.getStats).toHaveBeenCalledWith(query);
    expect(facade.state().kind).toBe('ready');
    expect(facade.totalEvents()).toBe(4);
    expect(facade.anonymousEvents()).toBe(3);
    expect(facade.authenticatedEvents()).toBe(1);
    expect(facade.channels()).toEqual([{ key: 'Copy', count: 2 }]);
    expect(facade.topTargets()[0].targetTitle).toBe('Test Park');
  });

  it('keeps previous stats available when a reload fails', () => {
    port.getStats.and.returnValue(of(stats));
    facade.load();

    port.getStats.and.returnValue(throwError(() => new Error('network')) as Observable<SocialShareStatsResult>);
    facade.load();

    expect(facade.state().kind).toBe('error');
    expect(facade.totalEvents()).toBe(4);
  });
});
