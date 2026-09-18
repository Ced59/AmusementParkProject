import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { skip } from 'rxjs';

import {
  TripItemDecisionStatus,
  TripPreferenceCompatibility
} from '@app/models/trips/trip.models';
import { TranslationService } from '@app/services/translation.service';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import {
  findNearestLanguageActivatedRoute,
  resolveLanguageFromActivatedRoute,
  resolveLanguageFromParamMap
} from '@shared/utils/routing/route-language.utils';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import {
  TripPreferenceSummaryFacade,
  TripPreferenceSummaryFilter
} from '../../state/trip-preference-summary.facade';

@Component({
  selector: 'app-trip-preference-summary-page',
  templateUrl: './trip-preference-summary-page.component.html',
  styleUrl: './trip-preference-summary-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [TripPreferenceSummaryFacade],
  imports: [
    DatePipe,
    RouterLink,
    TranslateModule,
    ImageDisplayComponent,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class TripPreferenceSummaryPageComponent implements OnInit {
  protected readonly currentLanguage = signal<string>('en');
  protected readonly filters: readonly TripPreferenceSummaryFilter[] = [
    'All',
    'Conflict',
    'Mixed',
    'Unknown',
    'Consensus'
  ];
  protected readonly decisionStatuses: readonly TripItemDecisionStatus[] = [
    'Review',
    'Retained',
    'SplitGroup',
    'Optional',
    'Excluded'
  ];
  protected readonly imageWidths: readonly number[] = [120, 200, 320];
  protected tripPlanId: string = '';

  constructor(
    protected readonly facade: TripPreferenceSummaryFacade,
    private readonly route: ActivatedRoute,
    translationService: TranslationService,
    destroyRef: DestroyRef
  ) {
    this.currentLanguage.set(resolveLanguageFromActivatedRoute(
      route,
      translationService.getCurrentLang() || 'en'
    ));
    findNearestLanguageActivatedRoute(route)?.paramMap.pipe(
      skip(1),
      takeUntilDestroyed(destroyRef)
    ).subscribe((params: ParamMap): void => {
      this.currentLanguage.set(resolveLanguageFromParamMap(params, this.currentLanguage()));
    });
  }

  ngOnInit(): void {
    this.tripPlanId = this.route.snapshot.paramMap.get('tripId') ?? '';
    this.facade.load(this.tripPlanId);
  }

  protected filterKey(filter: TripPreferenceSummaryFilter): string {
    return `trips.preferenceSummary.filters.${filter.toLowerCase()}`;
  }

  protected compatibilityKey(compatibility: TripPreferenceCompatibility): string {
    return `trips.preferenceSummary.compatibility.${compatibility.toLowerCase()}`;
  }

  protected decisionKey(status: TripItemDecisionStatus): string {
    return `trips.preferenceSummary.decisions.${status.toLowerCase()}`;
  }

  protected compatibilityIcon(compatibility: TripPreferenceCompatibility): string {
    switch (compatibility) {
      case 'Conflict': return 'pi-comments';
      case 'Consensus': return 'pi-sparkles';
      case 'Mixed': return 'pi-chart-bar';
      default: return 'pi-question-circle';
    }
  }

  protected decisionIcon(status: TripItemDecisionStatus): string {
    switch (status) {
      case 'Retained': return 'pi-check-circle';
      case 'SplitGroup': return 'pi-directions';
      case 'Optional': return 'pi-clock';
      case 'Excluded': return 'pi-times-circle';
      default: return 'pi-comments';
    }
  }
}
