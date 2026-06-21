import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { skip } from 'rxjs/operators';

import { TechnicalPagesApiService } from '@data-access/technical-pages/technical-pages-api.service';
import { TechnicalPage } from '@app/models/technical-pages/technical-page';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { resolveLocalizedText } from '@shared/utils/localization';
import { findNearestLanguageActivatedRoute, resolveLanguageFromActivatedRoute, resolveLanguageFromParamMap } from '@shared/utils/routing/route-language.utils';

interface TechnicalPageGroup {
  categoryKey: string;
  categoryName: string;
  pages: TechnicalPage[];
}

@Component({
  selector: 'app-technical-pages-page',
  templateUrl: './technical-pages-page.component.html',
  styleUrls: ['./technical-pages-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink, TranslateModule]
})
export class TechnicalPagesPageComponent implements OnInit {
  protected readonly pages = signal<TechnicalPage[]>([]);
  protected readonly isLoading = signal<boolean>(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly currentLang = signal<string>('en');
  protected readonly groups: Signal<TechnicalPageGroup[]> = computed(() => this.buildGroups(this.pages()));
  private activeLanguage: string | null = null;

  constructor(
    private readonly technicalPagesApiService: TechnicalPagesApiService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly seoService: SeoService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    const initialLanguage: string = resolveLanguageFromActivatedRoute(this.route, this.translationService.getCurrentLang() || 'en');
    this.applyLanguage(initialLanguage);
    this.watchRouteLanguageChanges();
    this.loadPages();
  }

  protected title(page: TechnicalPage): string {
    return resolveLocalizedText(page.titles, this.currentLang(), page.slug);
  }

  protected summary(page: TechnicalPage): string {
    return resolveLocalizedText(page.summaries, this.currentLang(), '');
  }

  private loadPages(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.technicalPagesApiService.getAllPublicPages()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (pages: TechnicalPage[]): void => {
          this.pages.set(pages);
          this.isLoading.set(false);
        },
        error: (): void => {
          this.errorMessage.set('technicalPages.list.errorMessage');
          this.isLoading.set(false);
        }
      });
  }

  private buildGroups(pages: TechnicalPage[]): TechnicalPageGroup[] {
    const groupsByKey: Map<string, TechnicalPageGroup> = new Map<string, TechnicalPageGroup>();

    for (const page of pages) {
      const categoryKey: string = (page.categoryKey || 'other').trim().toLowerCase();
      const categoryName: string = resolveLocalizedText(page.categoryNames, this.currentLang(), categoryKey);
      const existingGroup: TechnicalPageGroup | undefined = groupsByKey.get(categoryKey);

      if (existingGroup) {
        existingGroup.pages.push(page);
      } else {
        groupsByKey.set(categoryKey, {
          categoryKey,
          categoryName,
          pages: [page]
        });
      }
    }

    return Array.from(groupsByKey.values())
      .map((group: TechnicalPageGroup) => ({
        ...group,
        pages: group.pages.slice().sort((left: TechnicalPage, right: TechnicalPage) => {
          const sortDelta: number = (left.sortOrder ?? 0) - (right.sortOrder ?? 0);
          if (sortDelta !== 0) {
            return sortDelta;
          }

          return this.title(left).localeCompare(this.title(right));
        })
      }))
      .sort((left: TechnicalPageGroup, right: TechnicalPageGroup) => left.categoryName.localeCompare(right.categoryName));
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
    this.seoService.applyRouteDefaults(this.router.url);
  }
}
