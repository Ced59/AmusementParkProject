import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Inject, Injectable, PLATFORM_ID } from '@angular/core';

import { CookieConsentService } from '@core/privacy/cookie-consent.service';
import { environment } from '../../../environments/environment';
import type { PassportProductAnalyticsPort } from './passport-product-analytics.port';
import { PassportProductEvent } from './passport-product-event.model';

@Injectable({ providedIn: 'root' })
export class MatomoPassportProductAnalyticsService implements PassportProductAnalyticsPort {
  private readonly isBrowser: boolean;

  constructor(
    @Inject(PLATFORM_ID) platformId: object,
    @Inject(DOCUMENT) private readonly document: Document,
    private readonly cookieConsentService: CookieConsentService
  ) {
    this.isBrowser = isPlatformBrowser(platformId);
  }

  track(event: PassportProductEvent): void {
    if (!this.canTrack()) {
      return;
    }

    const trackingUrl: URL = new URL(this.resolveTrackerUrl());
    trackingUrl.searchParams.set('idsite', environment.analytics.matomoSiteId.toString());
    trackingUrl.searchParams.set('rec', '1');
    trackingUrl.searchParams.set('apiv', '1');
    trackingUrl.searchParams.set('url', new URL('product/passport', environment.baseUrl).toString());
    trackingUrl.searchParams.set('action_name', 'Passport product event');
    trackingUrl.searchParams.set('e_c', 'Passport');
    trackingUrl.searchParams.set('e_a', event.type);
    trackingUrl.searchParams.set('e_n', this.buildSafeLabel(event));
    trackingUrl.searchParams.set('rand', `${Date.now()}-${Math.random().toString(36).slice(2)}`);

    const imageConstructor: typeof Image | undefined = this.document.defaultView?.Image;
    if (!imageConstructor) {
      return;
    }

    const trackingPixel: HTMLImageElement = new imageConstructor(1, 1);
    trackingPixel.referrerPolicy = 'strict-origin-when-cross-origin';
    trackingPixel.src = trackingUrl.toString();
  }

  private canTrack(): boolean {
    return this.isBrowser
      && environment.analytics.matomoEnabled
      && environment.analytics.matomoTrackerUrl.trim().length > 0
      && (!environment.analytics.matomoRequireConsent
        || this.cookieConsentService.hasAcceptedOptionalCookies());
  }

  private buildSafeLabel(event: PassportProductEvent): string {
    const properties: string[] = [`source=${event.source}`];
    if ('datePrecision' in event) {
      properties.push(`date-precision=${event.datePrecision.toLowerCase()}`);
    }
    if ('countBucket' in event) {
      properties.push(`count=${event.countBucket}`);
    }
    if ('targetType' in event) {
      properties.push(`target=${event.targetType}`);
    }
    if ('scope' in event) {
      properties.push(`scope=${event.scope}`);
    }
    if ('format' in event) {
      properties.push(`format=${event.format.toLowerCase()}`);
    }

    return properties.join(';');
  }

  private resolveTrackerUrl(): string {
    const trackerUrl: string = environment.analytics.matomoTrackerUrl.endsWith('/')
      ? environment.analytics.matomoTrackerUrl
      : `${environment.analytics.matomoTrackerUrl}/`;
    return `${trackerUrl}matomo.php`;
  }
}
