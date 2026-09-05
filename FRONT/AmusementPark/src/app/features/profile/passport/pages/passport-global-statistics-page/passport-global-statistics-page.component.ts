import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import {
  PassportGlobalFilterPark,
  PassportGlobalStatistics
} from '@app/models/passport/passport-statistics.models';
import { TranslationService } from '@app/services/translation.service';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { PassportGlobalBarChartComponent } from '../../components/passport-global-bar-chart/passport-global-bar-chart.component';
import { PassportRatingEvolutionChartComponent } from '../../components/passport-rating-evolution-chart/passport-rating-evolution-chart.component';
import { PassportGlobalBarChartRow } from '../../models/passport-global-chart.models';
import { PassportGlobalStatisticsStateFacade } from '../../state/passport-global-statistics-state.facade';

@Component({
  selector: 'app-passport-global-statistics-page',
  templateUrl: './passport-global-statistics-page.component.html',
  styleUrl: './passport-global-statistics-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PassportGlobalStatisticsStateFacade],
  imports: [
    TranslateModule,
    PageStateComponent,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective,
    PassportGlobalBarChartComponent,
    PassportRatingEvolutionChartComponent
  ]
})
export class PassportGlobalStatisticsPageComponent implements OnInit {
  constructor(
    protected readonly facade: PassportGlobalStatisticsStateFacade,
    private readonly router: Router,
    private readonly translationService: TranslationService
  ) {
  }

  public ngOnInit(): void {
    this.facade.load();
  }

  protected backToPassport(): void {
    void this.router.navigate(['/', this.currentLanguage(), 'profile', 'passport']);
  }

  protected onYearChange(value: string): void {
    this.facade.selectYear(value ? Number(value) : null);
  }

  protected onParkChange(value: string, parks: PassportGlobalFilterPark[]): void {
    const index: number = Number(value);
    this.facade.selectPark(Number.isInteger(index) && index >= 0 ? parks[index]?.parkId ?? null : null);
  }

  protected selectedParkIndex(statistics: PassportGlobalStatistics): string {
    const index: number = statistics.availableParks.findIndex(
      (park: PassportGlobalFilterPark): boolean => park.parkId === this.facade.filter().parkId
    );
    return index < 0 ? '' : String(index);
  }

  protected hasFilters(): boolean {
    return this.facade.filter().year !== null || this.facade.filter().parkId !== null;
  }

  protected activityRows(statistics: PassportGlobalStatistics): PassportGlobalBarChartRow[] {
    return statistics.activityByYear.map((item) => ({
      id: String(item.year),
      label: String(item.year),
      primaryValue: item.visitCount,
      secondaryValue: item.recordedRideCount
    }));
  }

  protected parkRows(statistics: PassportGlobalStatistics): PassportGlobalBarChartRow[] {
    return statistics.topParks.map((item) => ({
      id: item.parkId,
      label: item.parkName,
      fallbackLabelKey: 'passport.globalStatistics.unavailablePark',
      primaryValue: item.visitCount,
      secondaryValue: item.recordedRideCount
    }));
  }

  protected itemRows(statistics: PassportGlobalStatistics): PassportGlobalBarChartRow[] {
    return statistics.topItems.map((item) => ({
      id: item.parkItemId,
      label: item.parkItemName,
      fallbackLabelKey: 'passport.globalStatistics.unavailableItem',
      detail: item.parkName,
      primaryValue: item.completedRideCount
    }));
  }

  protected outcomeRows(statistics: PassportGlobalStatistics): PassportGlobalBarChartRow[] {
    const outcomes = statistics.summary.rideOutcomes;
    return [
      { id: 'completed', label: null, fallbackLabelKey: 'passport.statistics.outcomes.completed', primaryValue: outcomes.completedRideCount },
      { id: 'attempted', label: null, fallbackLabelKey: 'passport.statistics.outcomes.attempted', primaryValue: outcomes.attemptedCount },
      { id: 'closed', label: null, fallbackLabelKey: 'passport.statistics.outcomes.missedClosed', primaryValue: outcomes.missedClosedCount },
      { id: 'unavailable', label: null, fallbackLabelKey: 'passport.statistics.outcomes.missedUnavailable', primaryValue: outcomes.missedUnavailableCount },
      { id: 'skipped', label: null, fallbackLabelKey: 'passport.statistics.outcomes.skippedByChoice', primaryValue: outcomes.skippedByChoiceCount }
    ];
  }

  protected coveragePercent(rate: number): string {
    return new Intl.NumberFormat(this.currentLanguage(), {
      style: 'percent',
      maximumFractionDigits: 0
    }).format(rate);
  }

  private currentLanguage(): string {
    return this.translationService.getCurrentLang() || this.router.url.split('/')[1] || 'en';
  }
}
