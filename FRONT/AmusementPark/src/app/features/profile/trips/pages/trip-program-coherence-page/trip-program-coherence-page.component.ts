import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { skip } from 'rxjs';

import {
  TripProgramCoherenceIssue,
  TripProgramCoherenceSeverity,
  TripProgramOpeningState
} from '@app/models/trips/trip.models';
import { TranslationService } from '@app/services/translation.service';
import { getAttractionStatusValueKey } from '@shared/utils/display/park-item-presentation.helpers';
import {
  findNearestLanguageActivatedRoute,
  resolveLanguageFromActivatedRoute,
  resolveLanguageFromParamMap
} from '@shared/utils/routing/route-language.utils';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { TripProgramCoherenceFacade } from '../../state/trip-program-coherence.facade';

@Component({
  selector: 'app-trip-program-coherence-page',
  templateUrl: './trip-program-coherence-page.component.html',
  styleUrl: './trip-program-coherence-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [TripProgramCoherenceFacade],
  imports: [
    DatePipe,
    DecimalPipe,
    RouterLink,
    TranslateModule,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class TripProgramCoherencePageComponent implements OnInit {
  protected readonly currentLanguage = signal<string>('en');
  protected tripPlanId: string = '';

  constructor(
    protected readonly facade: TripProgramCoherenceFacade,
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

  protected issueKey(issue: TripProgramCoherenceIssue): string {
    return `trips.coherence.issues.${lowerFirst(issue.code)}`;
  }

  protected severityKey(severity: TripProgramCoherenceSeverity): string {
    return `trips.coherence.severity.${severity.toLowerCase()}`;
  }

  protected openingKey(state: TripProgramOpeningState): string {
    return `trips.coherence.opening.${state.toLowerCase()}`;
  }

  protected severityIcon(severity: TripProgramCoherenceSeverity): string {
    switch (severity) {
      case 'Critical': return 'pi-exclamation-triangle';
      case 'Attention': return 'pi-info-circle';
      case 'Information': return 'pi-refresh';
    }
  }

  protected parkStatusKey(status: string | null): string | null {
    switch (status) {
      case 'Operating': return 'parks.statuses.operating';
      case 'Planned': return 'parks.statuses.planned';
      case 'UnderConstruction': return 'parks.statuses.underConstruction';
      case 'TemporarilyClosed': return 'parks.statuses.temporarilyClosed';
      case 'ClosedDefinitively': return 'parks.statuses.closedDefinitively';
      case 'Cancelled': return 'parks.statuses.cancelled';
      default: return null;
    }
  }

  protected attractionStatusKey(status: string | null): string | null {
    return getAttractionStatusValueKey(status);
  }

}

function lowerFirst(value: string): string {
  return value.length === 0 ? value : `${value[0].toLowerCase()}${value.slice(1)}`;
}
