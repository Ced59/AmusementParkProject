import { CookieConsentService } from '@core/privacy/cookie-consent.service';
import { environment } from '../../../environments/environment';
import { MatomoShareProductAnalyticsService } from './matomo-share-product-analytics.service';

describe('MatomoShareProductAnalyticsService', () => {
  const initialEnabled: boolean = environment.analytics.matomoEnabled;
  const trackedUrls: string[] = [];
  const referrerPolicies: string[] = [];
  let accepted: boolean;
  let documentStub: Document;
  let consentStub: CookieConsentService;

  beforeEach(() => {
    trackedUrls.length = 0;
    referrerPolicies.length = 0;
    accepted = true;
    environment.analytics.matomoEnabled = true;

    const TrackingImage = function(this: HTMLImageElement): void {
      this.referrerPolicy = '';
      Object.defineProperty(this, 'src', {
        set: (value: string): void => {
          referrerPolicies.push(this.referrerPolicy);
          trackedUrls.push(value);
        }
      });
    } as unknown as typeof Image;

    documentStub = {
      defaultView: {
        Image: TrackingImage
      }
    } as unknown as Document;
    consentStub = {
      hasAcceptedOptionalCookies: (): boolean => accepted
    } as CookieConsentService;
  });

  afterAll(() => {
    environment.analytics.matomoEnabled = initialEnabled;
  });

  it('sends only the bounded event name and recap type after consent', () => {
    const service = new MatomoShareProductAnalyticsService(
      'browser' as unknown as object,
      documentStub,
      consentStub
    );

    service.track({ type: 'share_opened', recapType: 'visit-recap' });

    expect(trackedUrls).toHaveLength(1);
    const trackingUrl: URL = new URL(trackedUrls[0]);
    expect(trackingUrl.searchParams.get('url')).toBe('http://localhost:4200/product/share');
    expect(trackingUrl.searchParams.get('e_c')).toBe('Share');
    expect(trackingUrl.searchParams.get('e_a')).toBe('share_opened');
    expect(trackingUrl.searchParams.get('e_n')).toBe('recap-type=visit-recap');
    expect(referrerPolicies).toEqual(['no-referrer']);
    expect(trackingUrl.toString()).not.toContain('shareId');
    expect(trackingUrl.toString()).not.toContain('visitId');
    expect(trackingUrl.toString()).not.toContain('rating');
    expect(trackingUrl.toString()).not.toContain('comment');
  });

  it('does not track when optional cookies are refused', () => {
    accepted = false;
    const service = new MatomoShareProductAnalyticsService(
      'browser' as unknown as object,
      documentStub,
      consentStub
    );

    service.track({ type: 'share_published', recapType: 'passport-profile' });

    expect(trackedUrls).toHaveLength(0);
  });

  it('does not track during server-side rendering', () => {
    const service = new MatomoShareProductAnalyticsService(
      'server' as unknown as object,
      documentStub,
      consentStub
    );

    service.track({ type: 'share_render_failed', recapType: 'year-recap' });

    expect(trackedUrls).toHaveLength(0);
  });
});
