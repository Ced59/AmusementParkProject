import { PublicLiveQueue } from '@app/models/live-data/public-live.models';
import { PublicLivePanelComponent } from './public-live-panel.component';

describe('PublicLivePanelComponent queue presentation', () => {
  it('formats return windows in the park timezone', () => {
    const component: PublicLivePanelComponent = new PublicLivePanelComponent();
    component.currentLanguage = 'fr';
    component.timeZoneId = 'Europe/Paris';
    const view = component as unknown as {
      formatQueueTime: (value: string | null) => string | null;
    };

    expect(view.formatQueueTime('2026-09-29T12:00:00Z')).toContain('14:00');
    expect(view.formatQueueTime(null)).toBeNull();
  });

  it('labels queue timestamps as UTC when the park timezone is unavailable', () => {
    const component: PublicLivePanelComponent = new PublicLivePanelComponent();
    component.currentLanguage = 'en';
    component.timeZoneId = null;
    const view = component as unknown as {
      formatQueueTime: (value: string | null) => string | null;
    };

    expect(view.formatQueueTime('2026-09-29T12:00:00Z')).toMatch(/12:00.*UTC/);
  });

  it('formats queue prices from minor units and rejects incomplete prices', () => {
    const component: PublicLivePanelComponent = new PublicLivePanelComponent();
    component.currentLanguage = 'fr';
    const view = component as unknown as {
      formatQueuePrice: (queue: PublicLiveQueue) => string | null;
    };
    const queue: PublicLiveQueue = createQueue();

    expect(view.formatQueuePrice(queue)).toMatch(/12[,.]50/);
    expect(view.formatQueuePrice({ ...queue, currencyCode: null })).toBeNull();
  });
});

function createQueue(): PublicLiveQueue {
  return {
    kind: 'PaidReturnTime',
    waitTimeMinutes: null,
    isEstimated: false,
    availability: 'Available',
    returnStartUtc: '2026-09-29T12:00:00Z',
    returnEndUtc: '2026-09-29T13:00:00Z',
    currentGroupStart: null,
    currentGroupEnd: null,
    nextAllocationUtc: null,
    priceMinorUnits: 1_250,
    currencyCode: 'EUR'
  };
}
