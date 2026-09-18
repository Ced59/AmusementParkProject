import { Injector } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { CookieConsentService } from '@core/privacy/cookie-consent.service';
import { environment } from '../../../environments/environment';
import { MicrosoftClarityTrackingService } from './microsoft-clarity-tracking.service';

describe('MicrosoftClarityTrackingService', () => {
  const initialClarityEnabled: boolean = environment.analytics.clarityEnabled;

  beforeEach(() => {
    environment.analytics.clarityEnabled = true;
    delete (window as unknown as { clarity?: unknown }).clarity;
  });

  afterEach(() => {
    environment.analytics.clarityEnabled = initialClarityEnabled;
    delete (window as unknown as { clarity?: unknown }).clarity;
  });

  it('does not initialize Clarity on a bearer invitation route', () => {
    const createElement = vi.fn();
    const invitationDocument: Document = {
      location: {
        pathname: '/fr/trip-invitations/opaque-secret'
      },
      createElement
    } as unknown as Document;
    const cookieConsentService: CookieConsentService = {
      hasAcceptedOptionalCookies: (): boolean => true
    } as unknown as CookieConsentService;
    const service = new MicrosoftClarityTrackingService(
      'browser' as unknown as object,
      invitationDocument,
      cookieConsentService,
      TestBed.inject(Injector)
    );

    service.initialize();

    expect(createElement).not.toHaveBeenCalled();
    expect((window as unknown as { clarity?: unknown }).clarity).toBeUndefined();
  });
});
