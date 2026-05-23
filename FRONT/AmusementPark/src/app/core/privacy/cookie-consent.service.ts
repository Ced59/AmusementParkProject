import { Inject, Injectable, PLATFORM_ID, Signal, WritableSignal, computed, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { MatomoTracker } from 'ngx-matomo-client';

import { environment } from '../../../environments/environment';
import { CookieConsentDecision, StoredCookieConsent } from './cookie-consent.model';

@Injectable({ providedIn: 'root' })
export class CookieConsentService {
  private readonly storageKey: string = 'amusementpark.cookie-consent.v1';
  private readonly legacyAnalyticsStorageKey: string = 'amusementpark.analytics-consent.v1';
  private readonly consentVersion: number = 1;
  private readonly isBrowser: boolean;
  private readonly decisionState: WritableSignal<CookieConsentDecision | null> = signal<CookieConsentDecision | null>(null);

  public readonly decision: Signal<CookieConsentDecision | null> = this.decisionState.asReadonly();
  public readonly hasAcceptedOptionalCookies: Signal<boolean> = computed((): boolean => this.decisionState() === 'accepted');
  public readonly isBannerVisible: Signal<boolean> = computed((): boolean => {
    return this.isCookieBannerEnabled() && this.decisionState() === null;
  });

  constructor(
    @Inject(PLATFORM_ID) platformId: object,
    private readonly tracker: MatomoTracker
  ) {
    this.isBrowser = isPlatformBrowser(platformId);
    this.decisionState.set(this.readStoredDecision());
    this.restoreOptionalCookieConsents();
  }

  public acceptOptionalCookies(): void {
    if (!this.isBrowser) {
      return;
    }

    this.writeStoredDecision('accepted');
    this.decisionState.set('accepted');

    if (environment.analytics.matomoEnabled && environment.analytics.matomoRequireConsent) {
      this.tracker.rememberConsentGiven(environment.analytics.matomoConsentHoursToExpire);
      this.tracker.trackPageView();
    }
  }

  public continueWithNecessaryCookiesOnly(): void {
    if (!this.isBrowser) {
      return;
    }

    this.writeStoredDecision('refused');
    this.decisionState.set('refused');
    this.forgetOptionalCookieConsents();
  }

  public revokeOptionalCookieConsent(): void {
    if (!this.isBrowser) {
      return;
    }

    this.writeStoredDecision('refused');
    this.decisionState.set('refused');
    this.forgetOptionalCookieConsents();
  }

  public resetCookieChoice(): void {
    if (!this.isBrowser) {
      return;
    }

    window.localStorage.removeItem(this.storageKey);
    window.localStorage.removeItem(this.legacyAnalyticsStorageKey);
    this.decisionState.set(null);
    this.forgetOptionalCookieConsents();
  }

  private restoreOptionalCookieConsents(): void {
    if (!this.isBrowser || !environment.analytics.matomoEnabled || !environment.analytics.matomoRequireConsent) {
      return;
    }

    if (this.decisionState() === 'accepted') {
      this.tracker.rememberConsentGiven(environment.analytics.matomoConsentHoursToExpire);
      return;
    }

    this.forgetOptionalCookieConsents();
  }

  private forgetOptionalCookieConsents(): void {
    if (!environment.analytics.matomoEnabled) {
      return;
    }

    this.tracker.forgetConsentGiven();
  }

  private readStoredDecision(): CookieConsentDecision | null {
    if (!this.isBrowser) {
      return null;
    }

    const currentDecision: CookieConsentDecision | null = this.readStoredDecisionFromKey(this.storageKey);
    if (currentDecision !== null) {
      return currentDecision;
    }

    const legacyDecision: CookieConsentDecision | null = this.readStoredDecisionFromKey(this.legacyAnalyticsStorageKey);
    if (legacyDecision !== null) {
      this.writeStoredDecision(legacyDecision);
      window.localStorage.removeItem(this.legacyAnalyticsStorageKey);
      return legacyDecision;
    }

    return null;
  }

  private readStoredDecisionFromKey(storageKey: string): CookieConsentDecision | null {
    const rawValue: string | null = window.localStorage.getItem(storageKey);
    if (!rawValue) {
      return null;
    }

    try {
      const storedConsent: Partial<StoredCookieConsent> = JSON.parse(rawValue) as Partial<StoredCookieConsent>;
      if (storedConsent.decision === 'accepted' || storedConsent.decision === 'refused') {
        return storedConsent.decision;
      }
    } catch {
      window.localStorage.removeItem(storageKey);
    }

    return null;
  }

  private writeStoredDecision(decision: CookieConsentDecision): void {
    const storedConsent: StoredCookieConsent = {
      decision,
      decidedAt: new Date().toISOString(),
      version: this.consentVersion
    };

    window.localStorage.setItem(this.storageKey, JSON.stringify(storedConsent));
  }

  private isCookieBannerEnabled(): boolean {
    return this.isBrowser && environment.analytics.consentBannerEnabled && environment.analytics.matomoRequireConsent;
  }
}
