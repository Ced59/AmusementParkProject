import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Inject, Injectable, PLATFORM_ID } from '@angular/core';

import { CookieConsentService } from '@core/privacy/cookie-consent.service';
import { environment } from '../../../environments/environment';
import {
  ProductAnalyticsPayload,
  serializeProductAnalyticsEvent
} from './product-analytics-contract';
import type { ConsentedProductAnalyticsEvent } from './product-analytics-event.model';
import type { ProductAnalyticsPort } from './product-analytics.port';

@Injectable({ providedIn: 'root' })
export class MatomoProductAnalyticsService implements ProductAnalyticsPort {
  private readonly isBrowser: boolean;

  constructor(
    @Inject(PLATFORM_ID) platformId: object,
    @Inject(DOCUMENT) private readonly document: Document,
    private readonly cookieConsentService: CookieConsentService
  ) {
    this.isBrowser = isPlatformBrowser(platformId);
  }

  track(event: ConsentedProductAnalyticsEvent): void {
    if (!this.canTrack()) {
      return;
    }

    const payload: ProductAnalyticsPayload | null = serializeProductAnalyticsEvent(event);
    if (payload === null || payload.channel !== 'matomo-consented') {
      return;
    }

    const trackingUrl: URL = new URL(this.resolveTrackerUrl());
    trackingUrl.searchParams.set('idsite', environment.analytics.matomoSiteId.toString());
    trackingUrl.searchParams.set('rec', '1');
    trackingUrl.searchParams.set('apiv', '1');
    trackingUrl.searchParams.set(
      'url',
      new URL(`product/${payload.route}`, environment.baseUrl).toString()
    );
    trackingUrl.searchParams.set('action_name', payload.actionName);
    trackingUrl.searchParams.set('e_c', payload.category);
    trackingUrl.searchParams.set('e_a', payload.action);
    trackingUrl.searchParams.set('e_n', payload.label);
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
