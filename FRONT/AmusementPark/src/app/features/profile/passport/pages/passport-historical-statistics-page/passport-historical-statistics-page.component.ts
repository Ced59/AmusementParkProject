import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import {
  PassportHistoricalCategory,
  PassportHistoricalParkEra,
  PassportHistoricalStatistics,
  PassportHistoricalTransformation
} from '@app/models/passport/passport-statistics.models';
import { TranslationService } from '@app/services/translation.service';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { getParkItemTypeTranslationKey } from '@shared/utils/display/display-label.helpers';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { PassportGlobalBarChartComponent } from '../../components/passport-global-bar-chart/passport-global-bar-chart.component';
import { PassportGlobalBarChartRow } from '../../models/passport-global-chart.models';
import { PassportHistoricalStatisticsStateFacade } from '../../state/passport-historical-statistics-state.facade';

@Component({
  selector: 'app-passport-historical-statistics-page',
  templateUrl: './passport-historical-statistics-page.component.html',
  styleUrl: './passport-historical-statistics-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PassportHistoricalStatisticsStateFacade],
  imports: [
    TranslateModule,
    PageStateComponent,
    PassportGlobalBarChartComponent,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class PassportHistoricalStatisticsPageComponent implements OnInit {
  constructor(
    protected readonly facade: PassportHistoricalStatisticsStateFacade,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly translateService: TranslateService
  ) {
  }

  public ngOnInit(): void {
    this.facade.load();
  }

  protected backToStatistics(): void {
    void this.router.navigate(['/', this.currentLanguage(), 'profile', 'passport', 'statistics']);
  }

  protected coveragePercent(rate: number): string {
    return new Intl.NumberFormat(this.currentLanguage(), {
      style: 'percent',
      maximumFractionDigits: 0
    }).format(rate);
  }

  protected parkEraRows(statistics: PassportHistoricalStatistics): PassportGlobalBarChartRow[] {
    return statistics.parksAcrossEras
      .filter((item: PassportHistoricalParkEra): boolean => item.canonicalEraCount > 0)
      .map((item: PassportHistoricalParkEra, index: number): PassportGlobalBarChartRow => ({
        id: String(index),
        label: item.parkName,
        fallbackLabelKey: 'passport.historicalStatistics.unknownPark',
        detail: this.yearRange(item.firstVisitYear, item.lastVisitYear),
        primaryValue: item.canonicalEraCount,
        secondaryValue: item.visitCount
      }));
  }

  protected categoryRows(statistics: PassportHistoricalStatistics): PassportGlobalBarChartRow[] {
    return statistics.historicalCategories.map(
      (item: PassportHistoricalCategory, index: number): PassportGlobalBarChartRow => ({
        id: String(index),
        label: this.translateService.instant(getParkItemTypeTranslationKey(item.category)),
        primaryValue: item.completedRideCount,
        secondaryValue: item.distinctAttractionCount
      })
    );
  }

  protected yearRange(firstYear: number, lastYear: number): string {
    return firstYear === lastYear ? String(firstYear) : `${firstYear} – ${lastYear}`;
  }

  protected classificationLabel(value: string): string {
    return this.translateService.instant(getParkItemTypeTranslationKey(value));
  }

  protected classificationList(values: string[]): string {
    return values.map((value: string): string => this.classificationLabel(value)).join(', ');
  }

  protected hasCategoryChange(item: PassportHistoricalTransformation): boolean {
    return item.categoriesAtVisit.some((category: string): boolean =>
      category.localeCompare(item.currentCategory, undefined, { sensitivity: 'accent' }) !== 0);
  }

  protected isEmpty(statistics: PassportHistoricalStatistics): boolean {
    return statistics.visitCount === 0;
  }

  private currentLanguage(): string {
    return this.translationService.getCurrentLang() || this.router.url.split('/')[1] || 'en';
  }
}
