import { Inject, Injectable, PLATFORM_ID, Signal, WritableSignal, computed, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

import { environment } from '../../../environments/environment';
import { CookieConsentDecision, StoredCookieConsent } from './cookie-consent.model';

@Injectable({ providedIn: 'root' })
export class CookieConsentService {
  private readonly storageKey: string = 'amusementpark.cookie-consent.v1';
  private readonly legacyAnalyticsStorageKey: string = 'amusementpark.analytics-consent.v1';
  private readonly consentCookieName: string = 'amusementpark_cookie_consent';
  private readonly consentVersion: number = 1;
  private readonly isBrowser: boolean;
  private readonly decisionState: WritableSignal<CookieConsentDecision | null> = signal<CookieConsentDecision | null>(null);

  public readonly decision: Signal<CookieConsentDecision | null> = this.decisionState.asReadonly();
  public readonly hasAcceptedOptionalCookies: Signal<boolean> = computed((): boolean => this.decisionState() === 'accepted');
  public readonly isBannerVisible: Signal<boolean> = computed((): boolean => {
    return this.isCookieBannerEnabled() && this.decisionState() === null;
  });

  constructor(
    @Inject(PLATFORM_ID) platformId: object
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
    this.writeConsentCookie('accepted');
    this.decisionState.set('accepted');

  }

  public continueWithNecessaryCookiesOnly(): void {
    if (!this.isBrowser) {
      return;
    }

    this.writeStoredDecision('refused');
    this.writeConsentCookie('refused');
    this.decisionState.set('refused');
    this.forgetOptionalCookieConsents();
  }

  public revokeOptionalCookieConsent(): void {
    if (!this.isBrowser) {
      return;
    }

    this.writeStoredDecision('refused');
    this.writeConsentCookie('refused');
    this.decisionState.set('refused');
    this.forgetOptionalCookieConsents();
  }

  public resetCookieChoice(): void {
    if (!this.isBrowser) {
      return;
    }

    window.localStorage.removeItem(this.storageKey);
    window.localStorage.removeItem(this.legacyAnalyticsStorageKey);
    this.deleteCookie(this.consentCookieName);
    this.decisionState.set(null);
    this.forgetOptionalCookieConsents();
  }

  private restoreOptionalCookieConsents(): void {
    if (!this.isBrowser || !environment.analytics.matomoEnabled || !environment.analytics.matomoRequireConsent) {
      return;
    }

    if (this.decisionState() === 'accepted') {
      return;
    }

    this.forgetOptionalCookieConsents();
  }

  private forgetOptionalCookieConsents(): void {
    if (!this.isBrowser) {
      return;
    }

    this.deleteMatomoCookies();
    this.deleteClarityCookies();
  }

  private deleteMatomoCookies(): void {
    if (!this.isBrowser) {
      return;
    }

    const cookieNames: string[] = document.cookie
      .split(';')
      .map((cookie: string): string => cookie.trim().split('=')[0])
      .filter((cookieName: string): boolean => cookieName.startsWith('_pk_'));

    for (const cookieName of cookieNames) {
      this.deleteCookie(cookieName);
    }
  }

  private deleteClarityCookies(): void {
    const clarityCookieNames: string[] = [
      '_clck',
      '_clsk',
      'CLID',
      'ANONCHK',
      'MR',
      'MUID',
      'SM'
    ];

    for (const cookieName of clarityCookieNames) {
      this.deleteCookie(cookieName);
    }
  }

  private readStoredDecision(): CookieConsentDecision | null {
    if (!this.isBrowser) {
      return null;
    }

    const currentDecision: CookieConsentDecision | null = this.readStoredDecisionFromKey(this.storageKey);
    if (currentDecision !== null) {
      this.writeConsentCookie(currentDecision);
      return currentDecision;
    }

    const legacyDecision: CookieConsentDecision | null = this.readStoredDecisionFromKey(this.legacyAnalyticsStorageKey);
    if (legacyDecision !== null) {
      this.writeStoredDecision(legacyDecision);
      this.writeConsentCookie(legacyDecision);
      window.localStorage.removeItem(this.legacyAnalyticsStorageKey);
      return legacyDecision;
    }

    const cookieDecision: CookieConsentDecision | null = this.readConsentCookieDecision();
    if (cookieDecision !== null) {
      this.writeStoredDecision(cookieDecision);
      return cookieDecision;
    }

    return null;
  }

  private readStoredDecisionFromKey(storageKey: string): CookieConsentDecision | null {
    const rawValue: string | null = window.localStorage.getItem(storageKey);
    if (!rawValue) {
      return null;
    }

    const normalizedRawValue: string = rawValue.trim().toLowerCase();
    if (normalizedRawValue === 'accepted' || normalizedRawValue === 'true') {
      return 'accepted';
    }

    if (normalizedRawValue === 'refused' || normalizedRawValue === 'false') {
      return 'refused';
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

  private writeConsentCookie(decision: CookieConsentDecision): void {
    if (!this.isBrowser) {
      return;
    }

    const maxAgeSeconds: number = Math.max(1, environment.analytics.matomoConsentHoursToExpire) * 60 * 60;
    const domainAttribute: string = this.resolveCookieDomainAttribute();
    document.cookie = `${this.consentCookieName}=${encodeURIComponent(decision)}; Max-Age=${maxAgeSeconds}; Path=/; SameSite=Lax${domainAttribute}`;
  }

  private readConsentCookieDecision(): CookieConsentDecision | null {
    if (!this.isBrowser) {
      return null;
    }

    const cookiePrefix: string = `${this.consentCookieName}=`;
    const rawCookie: string | undefined = document.cookie
      .split(';')
      .map((cookie: string): string => cookie.trim())
      .find((cookie: string): boolean => cookie.startsWith(cookiePrefix));

    if (!rawCookie) {
      return null;
    }

    const decision: string = decodeURIComponent(rawCookie.slice(cookiePrefix.length)).trim().toLowerCase();
    if (decision === 'accepted') {
      return 'accepted';
    }

    if (decision === 'refused') {
      return 'refused';
    }

    this.deleteCookie(this.consentCookieName);
    return null;
  }

  private deleteCookie(cookieName: string): void {
    if (!this.isBrowser || !cookieName) {
      return;
    }

    const domainAttribute: string = this.resolveCookieDomainAttribute();
    document.cookie = `${cookieName}=; Max-Age=0; Path=/; SameSite=Lax`;
    if (domainAttribute) {
      document.cookie = `${cookieName}=; Max-Age=0; Path=/; SameSite=Lax${domainAttribute}`;
    }
  }

  private resolveCookieDomainAttribute(): string {
    const hostname: string = window.location.hostname.toLowerCase();
    if (hostname === 'amusement-parks.fun' || hostname.endsWith('.amusement-parks.fun')) {
      return '; Domain=.amusement-parks.fun';
    }

    return '';
  }

  private isCookieBannerEnabled(): boolean {
    return this.isBrowser &&
      environment.analytics.consentBannerEnabled &&
      (environment.analytics.matomoRequireConsent || environment.analytics.clarityEnabled);
  }
}
