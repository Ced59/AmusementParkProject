import { ChangeDetectionStrategy, Component, computed, DestroyRef, OnInit, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Meta, Title } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { AboutStateFacade } from '@features/public/about/state/about-state.facade';
import { LocalizedPluralPipe } from '@shared/pipes';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { UiButtonDirective, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';

@Component({
  selector: 'app-about',
  templateUrl: './about.component.html',
  styleUrl: './about.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AboutStateFacade],
  imports: [
    RouterLink,
    TranslateModule,
    LocalizedPluralPipe,
    UiButtonDirective,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class AboutComponent implements OnInit {
  protected readonly currentLanguage = signal<string>('en');
  protected readonly visibleParkCount: Signal<number | null> = this.stateFacade.visibleParkCount;
  protected readonly formattedVisibleParkCount: Signal<string> = computed(() => {
    const count: number | null = this.visibleParkCount();

    if (count === null) {
      return '—';
    }

    return new Intl.NumberFormat(this.currentLanguage()).format(count);
  });

  constructor(
    private readonly title: Title,
    private readonly meta: Meta,
    private readonly translateService: TranslateService,
    private readonly destroyRef: DestroyRef,
    private readonly route: ActivatedRoute,
    private readonly stateFacade: AboutStateFacade
  ) {
  }

  ngOnInit(): void {
    this.updateCurrentLanguage();
    this.stateFacade.loadVisibleParkCount();

    this.translateService.stream(['aboutPage.seo.title', 'aboutPage.seo.description'])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((translations: Record<string, string>): void => {
        this.updateCurrentLanguage();

        const seoTitle: string = translations['aboutPage.seo.title'];
        const seoDescription: string = translations['aboutPage.seo.description'];

        this.title.setTitle(seoTitle);
        this.meta.updateTag({ name: 'description', content: seoDescription });
        this.meta.updateTag({ property: 'og:title', content: seoTitle });
        this.meta.updateTag({ property: 'og:description', content: seoDescription });
        this.meta.updateTag({ name: 'twitter:title', content: seoTitle });
        this.meta.updateTag({ name: 'twitter:description', content: seoDescription });
      });
  }

  private updateCurrentLanguage(): void {
    const fallbackLanguage: string = this.translateService.currentLang || 'en';
    this.currentLanguage.set(resolveLanguageFromActivatedRoute(this.route, fallbackLanguage));
  }
}
