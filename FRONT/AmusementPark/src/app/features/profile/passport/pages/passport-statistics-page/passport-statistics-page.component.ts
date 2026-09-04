import { ChangeDetectionStrategy, Component, DestroyRef, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Data, ParamMap, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { skip } from 'rxjs';

import { TranslationService } from '@app/services/translation.service';
import {
  findNearestLanguageActivatedRoute,
  resolveLanguageFromActivatedRoute,
  resolveLanguageFromParamMap
} from '@shared/utils/routing/route-language.utils';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { PassportRatingTimelineComponent } from '../../components/passport-rating-timeline/passport-rating-timeline.component';
import { PassportStatCardComponent } from '../../components/passport-stat-card/passport-stat-card.component';
import { PassportTableComponent } from '../../components/passport-table/passport-table.component';
import {
  PassportStatisticsNavigationViewModel,
  PassportStatisticsRouteScope,
  PassportStatisticsScopeKind
} from '../../models/passport-statistics-view.models';
import { PassportStatisticsStateFacade } from '../../state/passport-statistics-state.facade';

@Component({
  selector: 'app-passport-statistics-page',
  templateUrl: './passport-statistics-page.component.html',
  styleUrl: './passport-statistics-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PassportStatisticsStateFacade],
  imports: [
    TranslateModule,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective,
    PassportRatingTimelineComponent,
    PassportStatCardComponent,
    PassportTableComponent
  ]
})
export class PassportStatisticsPageComponent {
  protected readonly facade: PassportStatisticsStateFacade;
  protected readonly currentLanguage = signal<string>('en');
  private currentScope: PassportStatisticsRouteScope;

  constructor(
    facade: PassportStatisticsStateFacade,
    route: ActivatedRoute,
    private readonly router: Router,
    translationService: TranslationService,
    destroyRef: DestroyRef
  ) {
    this.facade = facade;
    const initialLanguage: string = resolveLanguageFromActivatedRoute(
      route,
      translationService.getCurrentLang() || 'en'
    );
    this.currentLanguage.set(initialLanguage);
    this.currentScope = this.resolveScope(route.snapshot.data, route.snapshot.paramMap);
    this.facade.load(this.currentScope, initialLanguage);

    route.paramMap.pipe(skip(1), takeUntilDestroyed(destroyRef)).subscribe((params: ParamMap): void => {
      const nextScope: PassportStatisticsRouteScope = this.resolveScope(route.snapshot.data, params);
      if (nextScope.kind === this.currentScope.kind && nextScope.targetId === this.currentScope.targetId) {
        return;
      }

      this.currentScope = nextScope;
      this.facade.load(nextScope, this.currentLanguage());
    });

    findNearestLanguageActivatedRoute(route)?.paramMap.pipe(
      skip(1),
      takeUntilDestroyed(destroyRef)
    ).subscribe((params: ParamMap): void => {
      const language: string = resolveLanguageFromParamMap(params, this.currentLanguage());
      if (language === this.currentLanguage()) {
        return;
      }

      this.currentLanguage.set(language);
      this.facade.changeLanguage(language);
    });
  }

  protected goBack(): void {
    void this.router.navigate(['/', this.currentLanguage(), 'profile']);
  }

  protected openVisit(visitId: string): void {
    this.navigate({ kind: 'visit', targetId: visitId, labelKey: '' });
  }

  protected navigate(navigation: PassportStatisticsNavigationViewModel): void {
    const base: string[] = ['/', this.currentLanguage(), 'profile'];
    if (navigation.kind === 'visit') {
      void this.router.navigate([...base, 'visits', navigation.targetId]);
      return;
    }

    const segment: string = navigation.kind === 'item'
      ? 'items'
      : navigation.kind === 'park'
        ? 'parks'
        : 'years';
    void this.router.navigate([...base, 'passport', segment, navigation.targetId]);
  }

  private resolveScope(data: Data, params: ParamMap): PassportStatisticsRouteScope {
    const kind: PassportStatisticsScopeKind = data['passportStatisticsScope'] as PassportStatisticsScopeKind;
    const parameterName: string = kind === 'item' ? 'parkItemId' : kind === 'park' ? 'parkId' : 'year';
    return { kind, targetId: params.get(parameterName)?.trim() ?? '' };
  }
}
