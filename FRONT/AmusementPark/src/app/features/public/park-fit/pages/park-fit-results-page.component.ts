import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import {
  ParkFitCriticalSource,
  ParkFitSearchPark,
  ParkFitSearchResponse,
  ParkFitScoreComponent
} from '@app/models/park-fit/park-fit-search.models';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { buildPublicParkRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { resolveLocalizedCountryName } from '@shared/utils/display/country-display.helpers';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import {
  formatParkFitDate,
  parkFitAvailabilityKey,
  parkFitComponentKindKey,
  parkFitComponentStateKey,
  parkFitConfidenceKey,
  parkFitMethodLabelKey,
  parkFitQualityKey,
  parkFitScoreReasonKey,
  parkFitScoreStateKey,
  parkFitSourceKindKey,
  resolveParkFitSourceSummary,
  resolveParkFitSourceUrl
} from '../mappers/park-fit-result-display.helpers';
import { ParkFitSearchFacade } from '../state/park-fit-search.facade';

@Component({
  selector: 'app-park-fit-results-page',
  templateUrl: './park-fit-results-page.component.html',
  styleUrl: './park-fit-results-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    TranslateModule,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class ParkFitResultsPageComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');
  protected readonly response: Signal<ParkFitSearchResponse | null> = this.facade.response;
  protected readonly parks: Signal<ParkFitSearchPark[]> = this.facade.visibleParks;
  protected readonly comparisonParks: Signal<ParkFitSearchPark[]> = this.facade.comparisonParks;
  protected readonly canCompare: Signal<boolean> = this.facade.canCompare;
  protected readonly comparisonLimitReached: Signal<boolean> = this.facade.comparisonLimitReached;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly facade: ParkFitSearchFacade,
    private readonly translationService: TranslationService,
    private readonly translateService: TranslateService,
    private readonly seoService: SeoService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    this.currentLang.set(resolveLanguageFromActivatedRoute(
      this.route,
      this.translationService.getCurrentLang() || 'en'
    ));
    this.applySeo();

    this.translationService.languageChanged
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((language: string): void => {
        this.currentLang.set(language);
        this.applySeo();
      });
  }

  protected criteriaRoute(): string[] {
    return ['/', this.currentLang(), 'park-fit'];
  }

  protected homeRoute(): string[] {
    return ['/', this.currentLang(), 'home'];
  }

  protected comparisonRoute(): string[] {
    return ['/', this.currentLang(), 'park-fit', 'compare'];
  }

  protected isSelectedForComparison(parkId: string): boolean {
    return this.comparisonParks().some((park: ParkFitSearchPark): boolean => park.parkId === parkId);
  }

  protected toggleComparison(parkId: string): void {
    this.facade.toggleComparisonPark(parkId);
  }

  protected parkRoute(park: ParkFitSearchPark): string[] | null {
    return buildPublicParkRouteCommands({
      language: this.currentLang(),
      parkId: park.parkId,
      parkName: park.parkName
    });
  }

  protected countryName(countryCode: string | null): string {
    return resolveLocalizedCountryName(countryCode, this.currentLang()) ?? '';
  }

  protected evaluationDateLabel(value: string): string {
    return this.formatDate(value);
  }

  protected verifiedDateLabel(value: string | null): string {
    return value ? this.formatDate(value) : this.translateService.instant('parkFit.results.proofs.unknownDate');
  }

  protected sourceSummary(source: ParkFitCriticalSource): string {
    return resolveParkFitSourceSummary(source, this.currentLang())
      ?? this.translateService.instant('parkFit.results.proofs.summaryFallback');
  }

  protected sourceUrl(source: ParkFitCriticalSource): string | null {
    return resolveParkFitSourceUrl(source);
  }

  protected rounded(value: number): number {
    return Math.round(value);
  }

  protected scoreStateKey(value: string): string {
    return parkFitScoreStateKey(value);
  }

  protected confidenceKey(value: string): string {
    return parkFitConfidenceKey(value);
  }

  protected availabilityKey(value: string): string {
    return parkFitAvailabilityKey(value);
  }

  protected qualityKey(value: string): string {
    return parkFitQualityKey(value);
  }

  protected componentKindKey(component: ParkFitScoreComponent): string {
    return parkFitComponentKindKey(component.kind);
  }

  protected componentStateKey(component: ParkFitScoreComponent): string {
    return parkFitComponentStateKey(component.state);
  }

  protected scoreReasonKey(value: string): string {
    return parkFitScoreReasonKey(value);
  }

  protected sourceKindKey(value: string): string {
    return parkFitSourceKindKey(value);
  }

  protected methodLabelKey(value: string): string {
    return parkFitMethodLabelKey(value);
  }

  private formatDate(value: string): string {
    return formatParkFitDate(value, this.currentLang())
      ?? this.translateService.instant('parkFit.results.proofs.unknownDate');
  }

  private applySeo(): void {
    this.seoService.applyParkFitResultsSeo(
      this.translateService.instant('parkFit.results.seo.title'),
      this.translateService.instant('parkFit.results.seo.description'),
      this.router.url,
      this.currentLang(),
      this.translateService.instant('parkFit.results.breadcrumb.home'),
      this.translateService.instant('parkFit.results.breadcrumb.parkFit'),
      this.translateService.instant('parkFit.results.breadcrumb.current')
    );
  }
}
