import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { skip } from 'rxjs';

import { TripExportDownloadService } from '@data-access/trips/trip-export-download.service';
import { TranslationService } from '@app/services/translation.service';
import {
  findNearestLanguageActivatedRoute,
  resolveLanguageFromActivatedRoute,
  resolveLanguageFromParamMap
} from '@shared/utils/routing/route-language.utils';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { TripExportFacade } from '../../state/trip-export.facade';

@Component({
  selector: 'app-trip-export-page',
  templateUrl: './trip-export-page.component.html',
  styleUrl: './trip-export-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [TripExportFacade],
  imports: [
    DatePipe,
    RouterLink,
    TranslateModule,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class TripExportPageComponent implements OnInit {
  protected readonly currentLanguage = signal<string>('en');
  protected tripPlanId: string = '';

  constructor(
    protected readonly facade: TripExportFacade,
    private readonly downloads: TripExportDownloadService,
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

  protected downloadJson(): void {
    const plan = this.facade.plan();
    if (plan) {
      this.downloads.downloadJson(plan);
    }
  }

  protected print(): void {
    this.downloads.print();
  }

  protected stateKey(state: string): string {
    return `trips.export.states.${state.toLowerCase()}`;
  }

  protected decisionKey(status: string): string {
    return `trips.export.decisions.${status.toLowerCase()}`;
  }
}
