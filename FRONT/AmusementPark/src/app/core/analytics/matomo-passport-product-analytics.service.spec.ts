import { CookieConsentService } from '@core/privacy/cookie-consent.service';
import { environment } from '../../../environments/environment';
import { MatomoPassportProductAnalyticsService } from './matomo-passport-product-analytics.service';

describe('MatomoPassportProductAnalyticsService', () => {
  const initialEnabled: boolean = environment.analytics.matomoEnabled;
  const trackedUrls: string[] = [];
  let accepted: boolean;
  let documentStub: Document;
  let consentStub: CookieConsentService;

  beforeEach(() => {
    trackedUrls.length = 0;
    accepted = true;
    environment.analytics.matomoEnabled = true;

    const TrackingImage = function(this: HTMLImageElement): void {
      this.referrerPolicy = '';
      Object.defineProperty(this, 'src', {
        set: (value: string): void => {
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

  it('sends a typed event with categorical properties only after consent', () => {
    const service = new MatomoPassportProductAnalyticsService(
      'browser' as unknown as object,
      documentStub,
      consentStub
    );

    service.track({
      type: 'ride_occurrence_added',
      source: 'authenticated',
      countBucket: 'two-to-five'
    });

    expect(trackedUrls).toHaveLength(1);
    const trackingUrl: URL = new URL(trackedUrls[0]);
    expect(trackingUrl.searchParams.get('url')).toBe('http://localhost:4200/product/passport');
    expect(trackingUrl.searchParams.get('e_c')).toBe('Passport');
    expect(trackingUrl.searchParams.get('e_a')).toBe('ride_occurrence_added');
    expect(trackingUrl.searchParams.get('e_n')).toBe('source=authenticated;count=two-to-five');
    expect(trackingUrl.toString()).not.toContain('visitId');
    expect(trackingUrl.toString()).not.toContain('parkId');
    expect(trackingUrl.toString()).not.toContain('targetId');
  });

  it('does not track when optional cookies are refused', () => {
    accepted = false;
    const service = new MatomoPassportProductAnalyticsService(
      'browser' as unknown as object,
      documentStub,
      consentStub
    );

    service.track({ type: 'passport_opened', source: 'anonymous-local' });

    expect(trackedUrls).toHaveLength(0);
  });

  it('does not track during server-side rendering', () => {
    const service = new MatomoPassportProductAnalyticsService(
      'server' as unknown as object,
      documentStub,
      consentStub
    );

    service.track({ type: 'passport_opened', source: 'authenticated' });

    expect(trackedUrls).toHaveLength(0);
  });
});
