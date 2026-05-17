import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter } from 'rxjs/operators';

import { TranslateModule } from '@ngx-translate/core';

import { TranslationService } from '@app/services/translation.service';
import { LANGUAGES, LanguageOption } from '@shared/models/localization';
import { resolveSupportedLanguageFromUrl } from '@shared/utils/routing/localized-route.helpers';

@Component({
  selector: 'app-public-footer',
  templateUrl: './public-footer.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule]
})
export class PublicFooterComponent implements OnInit {
  protected readonly languages: readonly LanguageOption[] = LANGUAGES;
  protected readonly selectedLanguage = signal<string>('en');
  protected readonly currentUrl = signal<string>('');
  protected readonly currentYear: number = new Date().getFullYear();

  constructor(
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    this.currentUrl.set(this.router.url);
    this.selectedLanguage.set(this.getLanguageFromUrl());

    this.router.events.pipe(
      filter((event: unknown): event is NavigationEnd => event instanceof NavigationEnd),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((event: NavigationEnd): void => {
      this.currentUrl.set(event.urlAfterRedirects);
      this.selectedLanguage.set(this.getLanguageFromUrl());
    });

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string): void => {
      this.selectedLanguage.set(language);
    });
  }

  protected languageRouteCommands(language: string): string[] {
    const routeSegments: string[] = this.getCurrentPathSegments();

    if (routeSegments.length === 0) {
      return ['/', language, 'home'];
    }

    if (this.isKnownLanguage(routeSegments[0])) {
      routeSegments[0] = language;
    } else {
      routeSegments.unshift(language);
    }

    return ['/', ...routeSegments];
  }

  protected isCurrentLanguage(language: string): boolean {
    return this.selectedLanguage() === language;
  }

  private getLanguageFromUrl(): string {
    return resolveSupportedLanguageFromUrl(this.currentUrl(), this.translationService.getCurrentLang() || 'en');
  }

  private getCurrentPathSegments(): string[] {
    const path: string = this.currentUrl().split('?')[0]?.split('#')[0] ?? '';
    return path.split('/').filter((segment: string): boolean => segment.length > 0);
  }

  private isKnownLanguage(language: string | undefined): boolean {
    return LANGUAGES.some((languageOption: LanguageOption): boolean => languageOption.value === language);
  }
}
