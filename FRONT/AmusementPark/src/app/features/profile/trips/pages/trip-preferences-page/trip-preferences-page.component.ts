import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { skip } from 'rxjs';

import {
  TripItemPreference,
  TripItemPreferenceLevel,
  TripItemPreferenceReason
} from '@app/models/trips/trip.models';
import { TranslationService } from '@app/services/translation.service';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import {
  findNearestLanguageActivatedRoute,
  resolveLanguageFromActivatedRoute,
  resolveLanguageFromParamMap
} from '@shared/utils/routing/route-language.utils';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { TripPreferencesFacade } from '../../state/trip-preferences.facade';

interface TripPreferenceGroup {
  parkId: string;
  parkName: string;
  items: TripItemPreference[];
}

@Component({
  selector: 'app-trip-preferences-page',
  templateUrl: './trip-preferences-page.component.html',
  styleUrl: './trip-preferences-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [TripPreferencesFacade],
  imports: [
    RouterLink,
    TranslateModule,
    ImageDisplayComponent,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class TripPreferencesPageComponent implements OnInit {
  protected readonly currentLanguage = signal<string>('en');
  protected readonly levels: readonly TripItemPreferenceLevel[] = [
    'MustDo',
    'WantToDo',
    'Optional',
    'NotForMe',
    'Unknown'
  ];
  protected readonly reasons: readonly TripItemPreferenceReason[] = [
    'Sensations',
    'Height',
    'AlreadyDone',
    'Unavailable',
    'Other'
  ];
  protected readonly imageWidths: readonly number[] = [120, 200, 320];
  protected tripPlanId: string = '';

  constructor(
    protected readonly facade: TripPreferencesFacade,
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

  protected groupedItems(): TripPreferenceGroup[] {
    const groups: Map<string, TripPreferenceGroup> = new Map<string, TripPreferenceGroup>();
    for (const item of this.facade.visibleItems()) {
      const group: TripPreferenceGroup = groups.get(item.parkId) ?? {
        parkId: item.parkId,
        parkName: item.parkName,
        items: []
      };
      group.items.push(item);
      groups.set(item.parkId, group);
    }
    return Array.from(groups.values());
  }

  protected count(level: TripItemPreferenceLevel): number {
    return this.facade.board()?.items.filter(
      (item: TripItemPreference): boolean => item.level === level
    ).length ?? 0;
  }

  protected levelKey(level: TripItemPreferenceLevel): string {
    return `trips.preferences.levels.${level.toLowerCase()}`;
  }

  protected reasonKey(reason: TripItemPreferenceReason): string {
    return `trips.preferences.reasons.${reason.toLowerCase()}`;
  }

  protected levelIcon(level: TripItemPreferenceLevel): string {
    switch (level) {
      case 'MustDo': return 'pi-bolt';
      case 'WantToDo': return 'pi-heart';
      case 'Optional': return 'pi-sparkles';
      case 'NotForMe': return 'pi-ban';
      default: return 'pi-minus-circle';
    }
  }

  protected parseLevel(value: string): TripItemPreferenceLevel | '' {
    return this.levels.includes(value as TripItemPreferenceLevel)
      ? value as TripItemPreferenceLevel
      : '';
  }

  protected parseReason(value: string): TripItemPreferenceReason | null {
    return this.reasons.includes(value as TripItemPreferenceReason)
      ? value as TripItemPreferenceReason
      : null;
  }
}
