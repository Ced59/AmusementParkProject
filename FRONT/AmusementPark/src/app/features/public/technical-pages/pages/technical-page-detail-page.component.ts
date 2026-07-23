import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, Inject, OnInit, effect, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { skip } from 'rxjs/operators';

import {
  TechnicalContentBlock,
  TechnicalContentLink,
  TechnicalContentListItem,
  TechnicalContentMetric,
  TechnicalContentTableCell,
  TechnicalPage
} from '@app/models/technical-pages/technical-page';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { applySsrPublicDataErrorStatus } from '@core/ssr/ssr-public-error-status';
import { SafeRichHtmlPipe } from '@shared/pipes';
import { resolveLocalizedText } from '@shared/utils/localization';
import { findNearestLanguageActivatedRoute, resolveLanguageFromActivatedRoute, resolveLanguageFromParamMap } from '@shared/utils/routing/route-language.utils';
import {
  PUBLIC_TECHNICAL_PAGES_API_SERVICE_PORT,
  PUBLIC_TECHNICAL_PAGES_IMAGES_API_SERVICE_PORT,
  PublicTechnicalPagesApiServicePort,
  PublicTechnicalPagesImagesApiServicePort
} from '../state/public-technical-pages-data.ports';

@Component({
  selector: 'app-technical-page-detail-page',
  templateUrl: './technical-page-detail-page.component.html',
  styleUrls: ['./technical-page-detail-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink, TranslateModule, SafeRichHtmlPipe]
})
export class TechnicalPageDetailPageComponent implements OnInit {
  protected readonly page = signal<TechnicalPage | null>(null);
  protected readonly isLoading = signal<boolean>(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly currentLang = signal<string>('en');
  private activeLanguage: string | null = null;

  constructor(
    @Inject(PUBLIC_TECHNICAL_PAGES_API_SERVICE_PORT) private readonly technicalPagesApiService: PublicTechnicalPagesApiServicePort,
    @Inject(PUBLIC_TECHNICAL_PAGES_IMAGES_API_SERVICE_PORT) private readonly imagesApiService: PublicTechnicalPagesImagesApiServicePort,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly seoService: SeoService,
    private readonly ssrHttpStatusService: SsrHttpStatusService,
    private readonly destroyRef: DestroyRef
  ) {
    effect((): void => {
      const currentPage: TechnicalPage | null = this.page();
      if (!currentPage) {
        return;
      }

      this.seoService.applyTechnicalPageSeo(currentPage, this.currentLang(), this.router.url);
    });
  }

  ngOnInit(): void {
    const initialLanguage: string = resolveLanguageFromActivatedRoute(this.route, this.translationService.getCurrentLang() || 'en');
    this.applyLanguage(initialLanguage);
    this.watchRouteLanguageChanges();

    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap): void => {
      const slug: string | null = params.get('slug');
      if (slug) {
        this.loadPage(slug);
      }
    });
  }

  protected title(page: TechnicalPage): string {
    return resolveLocalizedText(page.titles, this.currentLang(), page.slug);
  }

  protected summary(page: TechnicalPage): string {
    return resolveLocalizedText(page.summaries, this.currentLang(), '');
  }

  protected categoryName(page: TechnicalPage): string {
    return resolveLocalizedText(page.categoryNames, this.currentLang(), page.categoryKey);
  }

  protected blockTitle(block: TechnicalContentBlock): string {
    return resolveLocalizedText(block.titles, this.currentLang(), '');
  }

  protected blockBody(block: TechnicalContentBlock): string {
    return resolveLocalizedText(block.bodies, this.currentLang(), '');
  }

  protected blockCaption(block: TechnicalContentBlock): string {
    return resolveLocalizedText(block.captions, this.currentLang(), '');
  }

  protected blockAltText(block: TechnicalContentBlock): string {
    return resolveLocalizedText(block.altTexts, this.currentLang(), this.blockCaption(block) || this.blockTitle(block));
  }

  protected itemText(item: TechnicalContentListItem): string {
    return resolveLocalizedText(item.texts, this.currentLang(), '');
  }

  protected metricLabel(metric: TechnicalContentMetric): string {
    return resolveLocalizedText(metric.label, this.currentLang(), '');
  }

  protected metricValue(metric: TechnicalContentMetric): string {
    return resolveLocalizedText(metric.value, this.currentLang(), '');
  }

  protected metricHelpText(metric: TechnicalContentMetric): string {
    return resolveLocalizedText(metric.helpText, this.currentLang(), '');
  }

  protected linkLabel(link: TechnicalContentLink): string {
    return resolveLocalizedText(link.label, this.currentLang(), link.url);
  }

  protected cellText(cell: TechnicalContentTableCell): string {
    return resolveLocalizedText(cell.texts, this.currentLang(), '');
  }

  protected imageUrl(block: TechnicalContentBlock): string | null {
    return this.imagesApiService.resolveImageUrl(block.imageUrl || block.imageId || null, { width: 1280 });
  }

  protected normalizedBlockType(block: TechnicalContentBlock): string {
    return (block.blockType || 'richText').trim().toLowerCase();
  }

  protected blockTone(block: TechnicalContentBlock): string {
    const tone: string = (block.tone || 'neutral').trim().toLowerCase();
    return ['neutral', 'info', 'warning', 'success', 'danger'].includes(tone) ? tone : 'neutral';
  }

  protected hasText(block: TechnicalContentBlock): boolean {
    return this.blockTitle(block).length > 0 || this.blockBody(block).length > 0;
  }

  private loadPage(slug: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.page.set(null);

    this.technicalPagesApiService.getBySlug(slug)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page: TechnicalPage): void => {
          this.page.set(page);
          this.isLoading.set(false);
        },
        error: (error: unknown): void => {
          applySsrPublicDataErrorStatus(error, this.ssrHttpStatusService);
          this.seoService.applyNotFoundSeo(this.currentLang(), this.router.url);

          this.errorMessage.set('technicalPages.detail.errorMessage');
          this.isLoading.set(false);
        }
      });
  }

  private watchRouteLanguageChanges(): void {
    const languageRoute: ActivatedRoute | null = findNearestLanguageActivatedRoute(this.route);

    languageRoute?.paramMap.pipe(
      skip(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((params: ParamMap) => {
      const language: string = resolveLanguageFromParamMap(params, this.currentLang());
      this.applyLanguage(language);
    });
  }

  private applyLanguage(language: string): void {
    if (this.activeLanguage === language) {
      return;
    }

    this.activeLanguage = language;
    this.currentLang.set(language);
  }
}
