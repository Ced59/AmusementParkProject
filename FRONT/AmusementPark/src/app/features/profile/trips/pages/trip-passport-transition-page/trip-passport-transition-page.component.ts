import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  OnInit,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { skip } from 'rxjs';

import {
  TripPassportConfirmation,
  TripPassportTransitionFacade
} from '../../state/trip-passport-transition.facade';
import { TripPassportTransitionItem } from '@app/models/trips/trip-passport-transition.models';
import { TranslationService } from '@app/services/translation.service';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import {
  findNearestLanguageActivatedRoute,
  resolveLanguageFromActivatedRoute,
  resolveLanguageFromParamMap
} from '@shared/utils/routing/route-language.utils';
import {
  UiButtonDirective,
  UiChipComponent,
  UiKickerComponent,
  UiSurfaceDirective
} from '@ui/primitives';

@Component({
  selector: 'app-trip-passport-transition-page',
  templateUrl: './trip-passport-transition-page.component.html',
  styleUrl: './trip-passport-transition-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [TripPassportTransitionFacade],
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
export class TripPassportTransitionPageComponent implements OnInit {
  protected readonly currentLanguage = signal<string>('en');
  protected readonly selections = signal<Record<string, readonly string[]>>({});
  protected readonly imageWidths: readonly number[] = [160, 240, 360];
  protected tripPlanId: string = '';

  private handledConfirmationRevision: number = 0;

  constructor(
    protected readonly facade: TripPassportTransitionFacade,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
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

    effect((): void => {
      const transition = this.facade.transition();
      if (transition) {
        this.selections.update((current: Record<string, readonly string[]>): Record<string, readonly string[]> => {
          const initialized: Record<string, readonly string[]> = { ...current };
          let changed: boolean = false;
          for (const day of transition.days) {
            if (!(day.localDate in initialized)) {
              initialized[day.localDate] = day.attractions
                .filter((item: TripPassportTransitionItem): boolean => item.isPreselected)
                .map((item: TripPassportTransitionItem): string => item.parkItemId);
              changed = true;
            }
          }
          return changed ? initialized : current;
        });
      }

      const confirmation: TripPassportConfirmation | null = this.facade.confirmation();
      if (!confirmation || confirmation.revision <= this.handledConfirmationRevision) {
        return;
      }

      this.handledConfirmationRevision = confirmation.revision;
      void this.router.navigate([
        '/',
        this.currentLanguage(),
        'profile',
        'visits',
        confirmation.visitId
      ]);
    });
  }

  ngOnInit(): void {
    this.tripPlanId = this.route.snapshot.paramMap.get('tripId') ?? '';
    this.facade.load(this.tripPlanId);
  }

  protected toggle(localDate: string, item: TripPassportTransitionItem): void {
    const day = this.facade.transition()?.days.find(candidate => candidate.localDate === localDate);
    if (this.facade.confirmingDate() || day?.canResume) {
      return;
    }

    this.selections.update((current: Record<string, readonly string[]>): Record<string, readonly string[]> => {
      const selected: Set<string> = new Set(current[localDate] ?? []);
      if (selected.has(item.parkItemId)) {
        selected.delete(item.parkItemId);
      } else {
        selected.add(item.parkItemId);
      }
      return { ...current, [localDate]: Array.from(selected) };
    });
  }

  protected isSelected(localDate: string, parkItemId: string): boolean {
    return (this.selections()[localDate] ?? []).includes(parkItemId);
  }

  protected selectedCount(localDate: string): number {
    return this.selections()[localDate]?.length ?? 0;
  }

  protected confirm(localDate: string): void {
    this.facade.confirm(localDate, this.selections()[localDate] ?? []);
  }

  protected preferenceKey(item: TripPassportTransitionItem): string {
    return `trips.passportTransition.preferences.${item.ownPreference.toLowerCase()}`;
  }
}
