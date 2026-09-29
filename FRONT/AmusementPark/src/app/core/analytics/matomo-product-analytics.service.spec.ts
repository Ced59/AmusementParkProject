import { CookieConsentService } from '@core/privacy/cookie-consent.service';
import { environment } from '../../../environments/environment';
import { MatomoProductAnalyticsService } from './matomo-product-analytics.service';

describe('MatomoProductAnalyticsService', () => {
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

  it('sends passport and share events through one privacy-safe adapter', () => {
    const service = createService('browser');

    service.track({
      type: 'ride_occurrence_added',
      source: 'authenticated',
      countBucket: 'two-to-five'
    });
    service.track({ type: 'share_opened', recapType: 'visit-recap' });

    expect(trackedUrls).toHaveLength(2);
    const passportUrl: URL = new URL(trackedUrls[0]);
    const shareUrl: URL = new URL(trackedUrls[1]);
    expect(passportUrl.searchParams.get('url')).toBe('http://localhost:4200/product/passport');
    expect(passportUrl.searchParams.get('e_c')).toBe('Passport');
    expect(passportUrl.searchParams.get('e_a')).toBe('ride_occurrence_added');
    expect(passportUrl.searchParams.get('e_n')).toBe(
      'schema-version=1;source=authenticated;count-bucket=two-to-five'
    );
    expect(shareUrl.searchParams.get('url')).toBe('http://localhost:4200/product/share');
    expect(shareUrl.searchParams.get('e_c')).toBe('Share');
    expect(shareUrl.searchParams.get('e_n')).toBe(
      'schema-version=1;recap-type=visit-recap'
    );
    expect(referrerPolicies).toEqual(['no-referrer', 'no-referrer']);
    expect(trackedUrls.join('')).not.toContain('visitId');
    expect(trackedUrls.join('')).not.toContain('parkId');
    expect(trackedUrls.join('')).not.toContain('shareId');
  });

  it('does not track when optional cookies are refused', () => {
    accepted = false;
    createService('browser').track({ type: 'passport_opened', source: 'anonymous-local' });

    expect(trackedUrls).toHaveLength(0);
  });

  it('does not track during server-side rendering', () => {
    createService('server').track({ type: 'share_render_failed', recapType: 'year-recap' });

    expect(trackedUrls).toHaveLength(0);
  });

  function createService(platformId: 'browser' | 'server'): MatomoProductAnalyticsService {
    return new MatomoProductAnalyticsService(
      platformId as unknown as object,
      documentStub,
      consentStub
    );
  }
});
