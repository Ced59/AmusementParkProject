import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, effect, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Data, Router, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import {
  PublicHistoricalCategoryNetChange,
  PublicHistoricalSubjectComparison,
  PublicParkHistoricalComparison
} from '@app/models/history/public-park-history.models';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { getParkItemCategoryTranslationKey } from '@shared/utils/display/display-label.helpers';
import {
  buildPublicParkHistoryRouteCommands,
  buildPublicParkRouteCommands,
  buildPublicRoutePath
} from '@shared/utils/routing/public-detail-route.helpers';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { ParkHistoryComparisonBreadcrumbSeoService } from '../state/park-history-comparison-breadcrumb-seo.service';
import { ParkHistoryComparisonStateFacade } from '../state/park-history-comparison-state.facade';
import {
  PARK_HISTORY_EXPLORER_ROUTE_DATA_KEY,
  ResolvedParkHistoricalComparisonRouteData
} from '../state/park-history-explorer.resolver';

@Component({
  selector: 'app-park-history-comparison-page',
  templateUrl: './park-history-comparison-page.component.html',
  styleUrls: ['./park-history-explorer.shared.scss', './park-history-comparison-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkHistoryComparisonStateFacade],
  imports: [FormsModule, PageStateComponent, RouterLink, TranslateModule]
})
export class ParkHistoryComparisonPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly comparison: Signal<PublicParkHistoricalComparison | undefined> = this.stateFacade.comparison;
  protected readonly opened = this.stateFacade.opened;
  protected readonly closed = this.stateFacade.closed;
  protected readonly changed = this.stateFacade.changed;
  protected readonly uncertain = this.stateFacade.uncertain;
  protected readonly stable = this.stateFacade.stable;
  protected readonly currentLanguage = signal<string>('en');
  protected selectedFromYear = new Date().getUTCFullYear() - 10;
  protected selectedToYear = new Date().getUTCFullYear();

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly stateFacade: ParkHistoryComparisonStateFacade,
    private readonly translationService: TranslationService,
    private readonly translateService: TranslateService,
    private readonly seoService: SeoService,
    private readonly breadcrumbSeoService: ParkHistoryComparisonBreadcrumbSeoService,
    private readonly destroyRef: DestroyRef
  ) {
    effect((): void => {
      const currentComparison: PublicParkHistoricalComparison | undefined = this.comparison();
      if (currentComparison) {
        this.applySeo(currentComparison);
      }
    });
  }

  ngOnInit(): void {
    this.currentLanguage.set(resolveLanguageFromActivatedRoute(
      this.route,
      this.translationService.getCurrentLang() || 'en'
    ));
    this.route.data
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((data: Data): void => {
        const resolved: ResolvedParkHistoricalComparisonRouteData | undefined =
          data[PARK_HISTORY_EXPLORER_ROUTE_DATA_KEY];
        this.selectedFromYear = resolved?.fromYear ?? this.selectedFromYear;
        this.selectedToYear = resolved?.toYear ?? this.selectedToYear;
        this.stateFacade.setResolvedComparison(resolved?.comparison ?? null);
      });
    this.translationService.languageChanged
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((language: string): void => {
        this.currentLanguage.set(language);
        const currentComparison: PublicParkHistoricalComparison | undefined = this.comparison();
        if (currentComparison) {
          this.applySeo(currentComparison);
        }
      });
  }

  protected compareSelectedYears(comparison: PublicParkHistoricalComparison): void {
    const fromYear: number = Number(this.selectedFromYear);
    const toYear: number = Number(this.selectedToYear);
    if (!this.isValidRange(fromYear, toYear)) {
      return;
    }

    this.router.navigate([...this.historyLink(comparison), 'compare', String(fromYear), String(toYear)]);
  }

  protected parkLink(comparison: PublicParkHistoricalComparison): string[] {
    return buildPublicParkRouteCommands({
      language: this.currentLanguage(),
      parkId: comparison.parkId,
      parkName: comparison.parkName
    }) ?? [];
  }

  protected historyLink(comparison: PublicParkHistoricalComparison): string[] {
    return buildPublicParkHistoryRouteCommands({
      language: this.currentLanguage(),
      parkId: comparison.parkId,
      parkName: comparison.parkName
    }) ?? [];
  }

  protected delta(change: PublicHistoricalCategoryNetChange): string {
    return change.netChange > 0 ? `+${change.netChange}` : String(change.netChange);
  }

  protected categoryLabel(change: PublicHistoricalCategoryNetChange): string {
    const key: string = getParkItemCategoryTranslationKey(change.category);
    const translated: string = this.translateService.instant(key);
    return translated === key ? change.category : translated;
  }

  protected barWidth(value: number, comparison: PublicParkHistoricalComparison): number {
    const maximum: number = Math.max(
      1,
      ...comparison.categoryNetChanges.flatMap(
        (change: PublicHistoricalCategoryNetChange): number[] => [change.fromCount, change.toCount]
      )
    );
    return Math.max(value > 0 ? 6 : 0, Math.round((value / maximum) * 100));
  }

  protected totalSources(subject: PublicHistoricalSubjectComparison): number {
    return Math.max(subject.fromSupportingSourceCount, subject.toSupportingSourceCount);
  }

  protected sourceLabel(subject: PublicHistoricalSubjectComparison): string {
    return this.translateService.instant('history.comparison.sources', { count: this.totalSources(subject) });
  }

  protected usesCurrentNameFallback(subject: PublicHistoricalSubjectComparison): boolean {
    return subject.nameOrigin === 'CurrentFallback';
  }

  protected rangeInvalid(): boolean {
    return !this.isValidRange(Number(this.selectedFromYear), Number(this.selectedToYear));
  }

  private isValidRange(fromYear: number, toYear: number): boolean {
    return Number.isInteger(fromYear)
      && Number.isInteger(toYear)
      && fromYear >= 1000
      && toYear <= 9999
      && fromYear < toYear;
  }

  private applySeo(comparison: PublicParkHistoricalComparison): void {
    const historyCommands: string[] = this.historyLink(comparison);
    const canonicalPath: string = buildPublicRoutePath([
      ...historyCommands,
      'compare',
      String(comparison.fromInstant.year),
      String(comparison.toInstant.year)
    ]) ?? '/';
    const title: string = this.translateService.instant('history.comparison.seoTitle', {
      park: comparison.parkName,
      from: comparison.fromInstant.year,
      to: comparison.toInstant.year
    });
    const description: string = this.translateService.instant('history.comparison.seoDescription', {
      park: comparison.parkName,
      from: comparison.fromInstant.year,
      to: comparison.toInstant.year
    });
    this.seoService.applyHistoryComparisonSeo(title, description, this.router.url, canonicalPath);
    this.breadcrumbSeoService.apply(
      comparison.parkId,
      comparison.parkName,
      historyCommands[4],
      this.currentLanguage(),
      canonicalPath,
      comparison.fromInstant.year,
      comparison.toInstant.year
    );
  }
}
