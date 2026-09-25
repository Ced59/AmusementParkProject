import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { skip } from 'rxjs';

import { TripActivityKind } from '@app/models/trips/trip-activity.models';
import { TranslationService } from '@app/services/translation.service';
import {
  findNearestLanguageActivatedRoute,
  resolveLanguageFromActivatedRoute,
  resolveLanguageFromParamMap
} from '@shared/utils/routing/route-language.utils';
import { UiButtonDirective, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { TripActivityFacade } from '../../state/trip-activity.facade';

@Component({
  selector: 'app-trip-activity-page',
  templateUrl: './trip-activity-page.component.html',
  styleUrl: './trip-activity-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [TripActivityFacade],
  imports: [DatePipe, RouterLink, TranslateModule, UiButtonDirective, UiKickerComponent, UiSurfaceDirective]
})
export class TripActivityPageComponent implements OnInit {
  protected readonly currentLanguage = signal<string>('en');
  protected tripPlanId: string = '';

  constructor(
    protected readonly facade: TripActivityFacade,
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

  protected activityKey(kind: TripActivityKind): string {
    return `trips.activity.kinds.${kind[0].toLowerCase()}${kind.slice(1)}`;
  }

  protected activityIcon(kind: TripActivityKind): string {
    if (kind.startsWith('Invitation') || kind.includes('Participant') || kind === 'OwnershipTransferred') {
      return 'pi-users';
    }
    if (kind === 'DatesChanged' || kind.startsWith('Day')) {
      return 'pi-calendar';
    }
    if (kind.startsWith('Candidate')) {
      return 'pi-map-marker';
    }
    if (kind.includes('Preference') || kind === 'CollectiveDecisionUpdated') {
      return 'pi-heart';
    }
    if (kind === 'PlanExported') {
      return 'pi-download';
    }
    return kind === 'TripCreated' ? 'pi-sparkles' : 'pi-pencil';
  }
}
