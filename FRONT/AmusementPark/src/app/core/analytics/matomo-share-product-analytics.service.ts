import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Inject, Injectable, PLATFORM_ID } from '@angular/core';

import { CookieConsentService } from '@core/privacy/cookie-consent.service';
import { environment } from '../../../environments/environment';
import type { ShareProductAnalyticsPort } from './share-product-analytics.port';
import { ShareProductEvent } from './share-product-event.model';

@Injectable({ providedIn: 'root' })
export class MatomoShareProductAnalyticsService implements ShareProductAnalyticsPort {
  private readonly isBrowser: boolean;

  constructor(
    @Inject(PLATFORM_ID) platformId: object,
    @Inject(DOCUMENT) private readonly document: Document,
    private readonly cookieConsentService: CookieConsentService
  ) {
    this.isBrowser = isPlatformBrowser(platformId);
  }

  track(event: ShareProductEvent): void {
    if (!this.canTrack()) {
      return;
    }

    const trackingUrl: URL = new URL(this.resolveTrackerUrl());
    trackingUrl.searchParams.set('idsite', environment.analytics.matomoSiteId.toString());
    trackingUrl.searchParams.set('rec', '1');
    trackingUrl.searchParams.set('apiv', '1');
    trackingUrl.searchParams.set('url', new URL('product/share', environment.baseUrl).toString());
    trackingUrl.searchParams.set('action_name', 'Share product event');
    trackingUrl.searchParams.set('e_c', 'Share');
    trackingUrl.searchParams.set('e_a', event.type);
    trackingUrl.searchParams.set('e_n', `recap-type=${event.recapType}`);
    trackingUrl.searchParams.set('rand', `${Date.now()}-${Math.random().toString(36).slice(2)}`);

    const imageConstructor: typeof Image | undefined = this.document.defaultView?.Image;
    if (!imageConstructor) {
      return;
    }

    const trackingPixel: HTMLImageElement = new imageConstructor(1, 1);
    trackingPixel.referrerPolicy = 'no-referrer';
    trackingPixel.src = trackingUrl.toString();
  }

  private canTrack(): boolean {
    return this.isBrowser
      && environment.analytics.matomoEnabled
      && environment.analytics.matomoTrackerUrl.trim().length > 0
      && (!environment.analytics.matomoRequireConsent
        || this.cookieConsentService.hasAcceptedOptionalCookies());
  }

  private resolveTrackerUrl(): string {
    const trackerUrl: string = environment.analytics.matomoTrackerUrl.endsWith('/')
      ? environment.analytics.matomoTrackerUrl
      : `${environment.analytics.matomoTrackerUrl}/`;
    return `${trackerUrl}matomo.php`;
  }
}
