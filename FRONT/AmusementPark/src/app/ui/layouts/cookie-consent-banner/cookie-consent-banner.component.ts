import { afterNextRender, ChangeDetectionStrategy, Component, computed, DestroyRef, OnInit, Signal, WritableSignal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { filter } from 'rxjs/operators';

import { CookieConsentService } from '@core/privacy/cookie-consent.service';
import { TranslationService } from '@app/services/translation.service';
import { resolveSupportedLanguageFromUrl } from '@shared/utils/routing/localized-route.helpers';

@Component({
  selector: 'app-cookie-consent-banner',
  templateUrl: './cookie-consent-banner.component.html',
  styleUrl: './cookie-consent-banner.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule]
})
export class CookieConsentBannerComponent implements OnInit {
  private readonly clientStateReady: WritableSignal<boolean> = signal<boolean>(false);
  protected readonly isVisible: Signal<boolean> = computed((): boolean => {
    // Keep the initial server and browser DOM identical. Restore the visitor's
    // existing choice after hydration without adding a late first-visit banner.
    return this.clientStateReady()
      ? this.cookieConsentService.isBannerVisible()
      : this.cookieConsentService.isConsentRequired;
  });
  protected readonly currentLanguage: WritableSignal<string> = signal<string>('en');

  constructor(
    private readonly cookieConsentService: CookieConsentService,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly destroyRef: DestroyRef
  ) {
    afterNextRender((): void => {
      this.clientStateReady.set(true);
    });
  }

  ngOnInit(): void {
    this.currentLanguage.set(this.getLanguageFromUrl());

    this.router.events.pipe(
      filter((event: unknown): event is NavigationEnd => event instanceof NavigationEnd),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((): void => {
      this.currentLanguage.set(this.getLanguageFromUrl());
    });
  }

  protected acceptOptionalCookies(): void {
    this.cookieConsentService.acceptOptionalCookies();
  }

  protected continueWithNecessaryCookiesOnly(): void {
    this.cookieConsentService.continueWithNecessaryCookiesOnly();
  }

  private getLanguageFromUrl(): string {
    return resolveSupportedLanguageFromUrl(this.router.url, this.translationService.getCurrentLang() || 'en');
  }
}
