import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Inject, Injectable, Injector, PLATFORM_ID, effect } from '@angular/core';

import { CookieConsentService } from '@core/privacy/cookie-consent.service';
import { environment } from '../../../environments/environment';

type ClarityStorageConsent = 'granted' | 'denied';
type ClarityCommand =
  | ['consentv2', { readonly ad_storage: ClarityStorageConsent; readonly analytics_storage: ClarityStorageConsent }]
  | [string, ...unknown[]];

interface ClarityWindow extends Window {
  clarity?: (...command: ClarityCommand) => void;
}

@Injectable({ providedIn: 'root' })
export class MicrosoftClarityTrackingService {
  private readonly isBrowser: boolean;
  private initialized: boolean = false;
  private scriptRequested: boolean = false;

  constructor(
    @Inject(PLATFORM_ID) platformId: object,
    @Inject(DOCUMENT) private readonly document: Document,
    private readonly cookieConsentService: CookieConsentService,
    private readonly injector: Injector
  ) {
    this.isBrowser = isPlatformBrowser(platformId);
  }

  public initialize(): void {
    if (this.initialized || !this.isBrowser || !environment.analytics.clarityEnabled || !environment.analytics.clarityProjectId) {
      return;
    }

    this.initialized = true;

    effect((): void => {
      if (this.cookieConsentService.hasAcceptedOptionalCookies()) {
        this.ensureTrackerLoaded();
        this.updateConsent('granted');
        return;
      }

      if (this.scriptRequested) {
        this.updateConsent('denied');
      }
    }, { injector: this.injector });

    if (this.cookieConsentService.hasAcceptedOptionalCookies()) {
      this.ensureTrackerLoaded();
      this.updateConsent('granted');
    }
  }

  private ensureTrackerLoaded(): void {
    this.configureQueue();

    if (this.scriptRequested) {
      return;
    }

    this.scriptRequested = true;

    const scriptElement: HTMLScriptElement = this.document.createElement('script');
    scriptElement.async = true;
    scriptElement.src = `https://www.clarity.ms/tag/${environment.analytics.clarityProjectId}`;

    const firstScript: HTMLScriptElement | null = this.document.getElementsByTagName('script')[0] ?? null;
    if (firstScript?.parentNode) {
      firstScript.parentNode.insertBefore(scriptElement, firstScript);
      return;
    }

    this.document.head.appendChild(scriptElement);
  }

  private configureQueue(): void {
    const clarityWindow: ClarityWindow = window as ClarityWindow;
    clarityWindow.clarity = clarityWindow.clarity ?? ((...command: ClarityCommand): void => {
      (clarityWindow.clarity as unknown as { q?: ClarityCommand[] }).q = (clarityWindow.clarity as unknown as { q?: ClarityCommand[] }).q ?? [];
      (clarityWindow.clarity as unknown as { q: ClarityCommand[] }).q.push(command);
    });
  }

  private updateConsent(storageConsent: ClarityStorageConsent): void {
    this.configureQueue();

    const clarityWindow: ClarityWindow = window as ClarityWindow;
    clarityWindow.clarity?.('consentv2', {
      ad_storage: 'denied',
      analytics_storage: storageConsent
    });
  }
}
