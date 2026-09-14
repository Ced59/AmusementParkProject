import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { ParkFitSearchPark } from '@app/models/park-fit/park-fit-search.models';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { buildPublicParkRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { buildParkFitComparisonSections } from '../mappers/park-fit-comparison.mapper';
import { formatParkFitDate } from '../mappers/park-fit-result-display.helpers';
import { ParkFitComparisonSection, ParkFitComparisonSelection } from '../models/park-fit-comparison.models';
import { ParkFitSearchFacade } from '../state/park-fit-search.facade';

@Component({
  selector: 'app-park-fit-comparison-page',
  templateUrl: './park-fit-comparison-page.component.html',
  styleUrl: './park-fit-comparison-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule, UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective]
})
export class ParkFitComparisonPageComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');
  protected readonly showOnlyDifferences = signal<boolean>(false);
  protected readonly selections: Signal<ParkFitComparisonSelection[]> = this.facade.comparisonSelections;
  protected readonly parks: Signal<ParkFitSearchPark[]> = computed(() =>
    this.selections().map((selection: ParkFitComparisonSelection): ParkFitSearchPark => selection.park)
  );
  protected readonly sections: Signal<ParkFitComparisonSection[]> = computed(() => {
    const sections: ParkFitComparisonSection[] = buildParkFitComparisonSections(
      this.parks(),
      (value: string | null): string => this.verifiedDateLabel(value)
    );
    if (!this.showOnlyDifferences()) {
      return sections;
    }

    return sections
      .map((section: ParkFitComparisonSection): ParkFitComparisonSection => ({
        ...section,
        rows: section.rows.filter((row): boolean => row.isDifferent)
      }))
      .filter((section: ParkFitComparisonSection): boolean => section.rows.length > 0);
  });

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

  protected homeRoute(): string[] {
    return ['/', this.currentLang(), 'home'];
  }

  protected criteriaRoute(): string[] {
    return ['/', this.currentLang(), 'park-fit'];
  }

  protected resultsRoute(): string[] {
    return ['/', this.currentLang(), 'park-fit', 'results'];
  }

  protected parkRoute(park: ParkFitSearchPark): string[] | null {
    return buildPublicParkRouteCommands({
      language: this.currentLang(),
      parkId: park.parkId,
      parkName: park.parkName
    });
  }

  protected toggleDifferences(): void {
    this.showOnlyDifferences.update((current: boolean): boolean => !current);
  }

  protected rounded(value: number): number {
    return Math.round(value);
  }

  private verifiedDateLabel(value: string | null): string {
    return value
      ? formatParkFitDate(value, this.currentLang()) ?? this.translateService.instant('parkFit.results.proofs.unknownDate')
      : this.translateService.instant('parkFit.results.proofs.unknownDate');
  }

  private applySeo(): void {
    this.seoService.applyParkFitComparisonSeo(
      this.translateService.instant('parkFit.comparison.seo.title'),
      this.translateService.instant('parkFit.comparison.seo.description'),
      this.router.url,
      this.currentLang(),
      this.translateService.instant('parkFit.results.breadcrumb.home'),
      this.translateService.instant('parkFit.results.breadcrumb.parkFit'),
      this.translateService.instant('parkFit.results.breadcrumb.current'),
      this.translateService.instant('parkFit.comparison.breadcrumb.current')
    );
  }
}
