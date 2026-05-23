import { Inject, Injectable, PLATFORM_ID, Signal, WritableSignal, computed, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { MatomoTracker } from 'ngx-matomo-client';

import { environment } from '../../../environments/environment';
import { AnalyticsConsentDecision, StoredAnalyticsConsent } from './analytics-consent.model';

@Injectable({ providedIn: 'root' })
export class AnalyticsConsentService {
  private readonly storageKey: string = 'amusementpark.analytics-consent.v1';
  private readonly isBrowser: boolean;
  private readonly decisionState: WritableSignal<AnalyticsConsentDecision | null> = signal<AnalyticsConsentDecision | null>(null);

  public readonly decision: Signal<AnalyticsConsentDecision | null> = this.decisionState.asReadonly();
  public readonly hasAcceptedAnalytics: Signal<boolean> = computed((): boolean => this.decisionState() === 'accepted');
  public readonly isBannerVisible: Signal<boolean> = computed((): boolean => {
    return this.isConsentBannerEnabled() && this.decisionState() === null;
  });

  constructor(
    @Inject(PLATFORM_ID) platformId: object,
    private readonly tracker: MatomoTracker
  ) {
    this.isBrowser = isPlatformBrowser(platformId);
    this.decisionState.set(this.readStoredDecision());
    this.restoreTrackerConsent();
  }

  public acceptAnalytics(): void {
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

  public continueWithoutAnalytics(): void {
    if (!this.isBrowser) {
      return;
    }

    this.writeStoredDecision('refused');
    this.decisionState.set('refused');
    this.forgetTrackerConsent();
  }

  public revokeAnalyticsConsent(): void {
    if (!this.isBrowser) {
      return;
    }

    this.writeStoredDecision('refused');
    this.decisionState.set('refused');
    this.forgetTrackerConsent();
  }

  public resetAnalyticsChoice(): void {
    if (!this.isBrowser) {
      return;
    }

    window.localStorage.removeItem(this.storageKey);
    this.decisionState.set(null);
    this.forgetTrackerConsent();
  }

  private restoreTrackerConsent(): void {
    if (!this.isBrowser || !environment.analytics.matomoEnabled || !environment.analytics.matomoRequireConsent) {
      return;
    }

    if (this.decisionState() === 'accepted') {
      this.tracker.rememberConsentGiven(environment.analytics.matomoConsentHoursToExpire);
      return;
    }

    this.forgetTrackerConsent();
  }

  private forgetTrackerConsent(): void {
    if (!environment.analytics.matomoEnabled) {
      return;
    }

    this.tracker.forgetConsentGiven();
  }

  private readStoredDecision(): AnalyticsConsentDecision | null {
    if (!this.isBrowser) {
      return null;
    }

    const rawValue: string | null = window.localStorage.getItem(this.storageKey);
    if (!rawValue) {
      return null;
    }

    try {
      const storedConsent: StoredAnalyticsConsent = JSON.parse(rawValue) as StoredAnalyticsConsent;
      if (storedConsent.decision === 'accepted' || storedConsent.decision === 'refused') {
        return storedConsent.decision;
      }
    } catch {
      window.localStorage.removeItem(this.storageKey);
    }

    return null;
  }

  private writeStoredDecision(decision: AnalyticsConsentDecision): void {
    const storedConsent: StoredAnalyticsConsent = {
      decision,
      decidedAt: new Date().toISOString()
    };

    window.localStorage.setItem(this.storageKey, JSON.stringify(storedConsent));
  }

  private isConsentBannerEnabled(): boolean {
    return this.isBrowser && environment.analytics.consentBannerEnabled && environment.analytics.matomoRequireConsent;
  }
}
