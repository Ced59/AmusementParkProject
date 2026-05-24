import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Inject, Injectable, Injector, NgZone, PLATFORM_ID, effect } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';

import { CookieConsentService } from '@core/privacy/cookie-consent.service';
import { environment } from '../../../environments/environment';

type MatomoQueueCommand = [string, ...unknown[]];

interface MatomoWindow extends Window {
  _paq?: MatomoQueueCommand[];
}

@Injectable({ providedIn: 'root' })
export class MatomoPageViewTrackingService {
  private readonly isBrowser: boolean;
  private initialized: boolean = false;
  private scriptRequested: boolean = false;
  private scriptLoaded: boolean = false;
  private linkTrackingEnabled: boolean = false;
  private lastTrackedUrl: string | null = null;
  private pendingTimeoutId: ReturnType<typeof setTimeout> | null = null;

  constructor(
    @Inject(PLATFORM_ID) platformId: object,
    @Inject(DOCUMENT) private readonly document: Document,
    private readonly router: Router,
    private readonly cookieConsentService: CookieConsentService,
    private readonly zone: NgZone,
    private readonly injector: Injector
  ) {
    this.isBrowser = isPlatformBrowser(platformId);
  }

  public initialize(): void {
    if (this.initialized || !this.isBrowser || !environment.analytics.matomoEnabled) {
      return;
    }

    this.initialized = true;

    this.router.events.pipe(
      filter((event: unknown): event is NavigationEnd => event instanceof NavigationEnd)
    ).subscribe((): void => {
      this.scheduleCurrentPageTracking();
    });

    effect((): void => {
      if (this.hasTrackingConsent()) {
        this.ensureTrackerLoaded();
        this.scheduleCurrentPageTracking();
        return;
      }

      this.lastTrackedUrl = null;
    }, { injector: this.injector });

    if (this.hasTrackingConsent()) {
      this.ensureTrackerLoaded();
      this.scheduleCurrentPageTracking();
    }
  }

  private scheduleCurrentPageTracking(): void {
    if (!this.hasTrackingConsent()) {
      return;
    }

    if (this.pendingTimeoutId !== null) {
      clearTimeout(this.pendingTimeoutId);
    }

    this.zone.runOutsideAngular((): void => {
      this.pendingTimeoutId = setTimeout((): void => {
        this.pendingTimeoutId = null;
        this.trackCurrentPage();
      }, 350);
    });
  }

  private trackCurrentPage(): void {
    if (!this.hasTrackingConsent()) {
      return;
    }

    this.ensureTrackerLoaded();

    const currentUrl: string = this.document.location.href;
    if (currentUrl === this.lastTrackedUrl) {
      return;
    }

    const pageTitle: string = this.document.title || 'AmusementPark';

    this.trackWithHttpApi(currentUrl, pageTitle, this.lastTrackedUrl);

    this.lastTrackedUrl = currentUrl;
  }

  private ensureTrackerLoaded(): void {
    this.configureQueueOnce();

    if (this.scriptRequested || !environment.analytics.matomoTrackerUrl) {
      return;
    }

    this.scriptRequested = true;

    const scriptElement: HTMLScriptElement = this.document.createElement('script');
    scriptElement.async = true;
    scriptElement.defer = true;
    scriptElement.src = this.resolveTrackerAssetUrl('matomo.js');
    scriptElement.onload = (): void => {
      this.scriptLoaded = true;
      this.trackCurrentPage();
    };
    scriptElement.onerror = (): void => {
      this.scriptRequested = false;
    };

    const firstScript: HTMLScriptElement | null = this.document.getElementsByTagName('script')[0] ?? null;
    if (firstScript?.parentNode) {
      firstScript.parentNode.insertBefore(scriptElement, firstScript);
      return;
    }

    this.document.head.appendChild(scriptElement);
  }

  private configureQueueOnce(): void {
    const queue: MatomoQueueCommand[] = this.getMatomoQueue();

    if (!this.linkTrackingEnabled) {
      queue.push(['setTrackerUrl', this.resolveTrackerAssetUrl('matomo.php')]);
      queue.push(['setSiteId', environment.analytics.matomoSiteId]);
      queue.push(['disableCampaignParameters']);
      queue.push(['enableLinkTracking']);
      this.linkTrackingEnabled = true;
    }
  }

  private trackWithHttpApi(currentUrl: string, pageTitle: string, referrerUrl: string | null): void {
    const trackingUrl: URL = new URL(this.resolveTrackerAssetUrl('matomo.php'));
    trackingUrl.searchParams.set('idsite', environment.analytics.matomoSiteId.toString());
    trackingUrl.searchParams.set('rec', '1');
    trackingUrl.searchParams.set('apiv', '1');
    trackingUrl.searchParams.set('url', currentUrl);
    trackingUrl.searchParams.set('action_name', pageTitle);
    trackingUrl.searchParams.set('rand', `${Date.now()}-${Math.random().toString(36).slice(2)}`);

    if (referrerUrl) {
      trackingUrl.searchParams.set('urlref', referrerUrl);
    }

    const trackingPixel: HTMLImageElement = new Image(1, 1);
    trackingPixel.referrerPolicy = 'strict-origin-when-cross-origin';
    trackingPixel.src = trackingUrl.toString();
  }

  private hasTrackingConsent(): boolean {
    return !environment.analytics.matomoRequireConsent || this.cookieConsentService.hasAcceptedOptionalCookies();
  }

  private getMatomoQueue(): MatomoQueueCommand[] {
    const matomoWindow: MatomoWindow = window as MatomoWindow;
    matomoWindow._paq = matomoWindow._paq ?? [];
    return matomoWindow._paq;
  }

  private resolveTrackerAssetUrl(assetName: 'matomo.js' | 'matomo.php'): string {
    const trackerUrl: string = environment.analytics.matomoTrackerUrl.endsWith('/')
      ? environment.analytics.matomoTrackerUrl
      : `${environment.analytics.matomoTrackerUrl}/`;

    return `${trackerUrl}${assetName}`;
  }
}
