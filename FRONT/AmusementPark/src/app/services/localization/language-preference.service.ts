import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Inject, Injectable, PLATFORM_ID, Signal, signal } from '@angular/core';

import {
  LANGUAGE_PREFERENCE_COOKIE_MAX_AGE_SECONDS,
  LANGUAGE_PREFERENCE_COOKIE_NAME,
  LANGUAGE_PREFERENCE_STORAGE_KEY
} from '@shared/models/localization';
import { isSupportedLanguage } from '@shared/utils/routing/localized-route.helpers';

@Injectable({
  providedIn: 'root'
})
export class LanguagePreferenceService {
  private readonly languageSignal = signal<string | null>(null);
  readonly preferredLanguage: Signal<string | null> = this.languageSignal.asReadonly();

  constructor(
    @Inject(PLATFORM_ID) private readonly platformId: object,
    @Inject(DOCUMENT) private readonly document: Document
  ) {
    this.languageSignal.set(this.readStoredPreference());
  }

  getPreferredLanguage(): string | null {
    return this.languageSignal();
  }

  setPreferredLanguage(language: string | null | undefined): string | null {
    const normalizedLanguage: string | null = this.normalizeLanguage(language);
    if (normalizedLanguage === null) {
      return null;
    }

    this.languageSignal.set(normalizedLanguage);
    this.writeLocalStoragePreference(normalizedLanguage);
    this.writeCookiePreference(normalizedLanguage);
    return normalizedLanguage;
  }

  private readStoredPreference(): string | null {
    if (!isPlatformBrowser(this.platformId)) {
      return null;
    }

    try {
      const localPreference: string | null = this.normalizeLanguage(
        this.document.defaultView?.localStorage.getItem(LANGUAGE_PREFERENCE_STORAGE_KEY)
      );
      if (localPreference !== null) {
        return localPreference;
      }
    } catch (_error) {
      // A cookie can still provide the preference when local storage is unavailable.
    }

    return this.readCookiePreference();
  }

  private readCookiePreference(): string | null {
    const cookiePrefix: string = `${LANGUAGE_PREFERENCE_COOKIE_NAME}=`;
    const cookieValue: string | undefined = this.document.cookie
      .split(';')
      .map((entry: string): string => entry.trim())
      .find((entry: string): boolean => entry.startsWith(cookiePrefix))
      ?.slice(cookiePrefix.length);

    if (!cookieValue) {
      return null;
    }

    try {
      return this.normalizeLanguage(decodeURIComponent(cookieValue));
    } catch (_error) {
      return null;
    }
  }

  private writeLocalStoragePreference(language: string): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    try {
      this.document.defaultView?.localStorage.setItem(LANGUAGE_PREFERENCE_STORAGE_KEY, language);
    } catch (_error) {
      // The preference signal and cookie remain available when local storage is blocked.
    }
  }

  private writeCookiePreference(language: string): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    const secureAttribute: string = this.document.location?.protocol === 'https:' ? '; Secure' : '';
    this.document.cookie = `${LANGUAGE_PREFERENCE_COOKIE_NAME}=${encodeURIComponent(language)}; Path=/; Max-Age=${LANGUAGE_PREFERENCE_COOKIE_MAX_AGE_SECONDS}; SameSite=Lax${secureAttribute}`;
  }

  private normalizeLanguage(language: string | null | undefined): string | null {
    const normalizedLanguage: string = (language ?? '').trim().toLowerCase().split(/[-_]/)[0] ?? '';
    return isSupportedLanguage(normalizedLanguage) ? normalizedLanguage : null;
  }
}
