import { TestBed } from '@angular/core/testing';

import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { SHARE_PRODUCT_ANALYTICS_PORT } from '@core/analytics/share-product-analytics.port';
import { ShareProductEvent } from '@core/analytics/share-product-event.model';
import { TranslateService } from '@ngx-translate/core';
import { FakeUserRankingSharePort } from './test-helpers/profile-ratings-panel.component/fake-user-ranking-share-port';
import { USER_RANKING_SHARE_PORT } from './user-ranking-share-state-data.ports';
import { UserRankingShareStateFacade } from './user-ranking-share-state.facade';

describe('UserRankingShareStateFacade', () => {
  it('tracks only successful steps of the personal ranking share lifecycle', () => {
    const analyticsEvents: ShareProductEvent[] = [];
    const sharePort: FakeUserRankingSharePort = new FakeUserRankingSharePort();
    TestBed.configureTestingModule({ providers: [
      UserRankingShareStateFacade,
      { provide: USER_RANKING_SHARE_PORT, useValue: sharePort },
      { provide: ToastMessageService, useValue: { add: vi.fn() } },
      { provide: TranslateService, useValue: { instant: (key: string): string => key } },
      {
        provide: SHARE_PRODUCT_ANALYTICS_PORT,
        useValue: {
          track: (event: ShareProductEvent): void => {
            analyticsEvents.push(event);
          }
        }
      }
    ] });
    const facade: UserRankingShareStateFacade = TestBed.inject(UserRankingShareStateFacade);

    facade.load();
    facade.openEditor();
    facade.preparePreview();
    facade.publishApprovedPreview();
    facade.rotate();
    facade.setPublic(false);

    expect(analyticsEvents).toEqual([
      { type: 'share_activation_started', recapType: 'personal-ranking' },
      { type: 'share_preview_created', recapType: 'personal-ranking' },
      { type: 'share_published', recapType: 'personal-ranking' },
      { type: 'share_rotated', recapType: 'personal-ranking' },
      { type: 'share_revoked', recapType: 'personal-ranking' }
    ]);
  });
});
