import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';

import { SeoService } from '@core/seo/seo.service';
import { TranslationService } from '@app/services/translation.service';

@Component({
  selector: 'app-public-not-found-page',
  templateUrl: './public-not-found-page.component.html',
  styleUrls: ['./public-not-found-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslatePipe]
})
export class PublicNotFoundPageComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');

  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  constructor(
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly seoService: SeoService
  ) {
  }

  ngOnInit(): void {
    const language: string = this.translationService.getCurrentLang() || this.resolveLanguageFromUrl();
    this.currentLang.set(language);
    this.seoService.applyNotFoundSeo(language, this.router.url);

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((changedLanguage: string) => {
      this.currentLang.set(changedLanguage);
      this.seoService.applyNotFoundSeo(changedLanguage, this.router.url);
    });
  }

  private resolveLanguageFromUrl(): string {
    const firstSegment: string | undefined = this.router.url.split('?')[0]?.split('/').filter((segment: string) => !!segment)[0];
    return firstSegment?.trim() || 'en';
  }
}
