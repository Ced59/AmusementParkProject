import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { TranslationService } from '@app/services/translation.service';
import { AdminContextualBlockAppliedEvent, AdminContextualBlockRefreshEvents } from '@features/admin/contextual-editing/state/admin-contextual-block-refresh-events.service';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { ParkReferenceKind } from '../models/park-reference-detail-view.model';
import { ParkReferenceDetailStateFacade } from '../state/park-reference-detail-state.facade';
import { ParkReferenceDetailViewComponent } from '../ui/park-reference-detail-view.component';

@Component({
  selector: 'app-park-reference-detail-page',
  templateUrl: './park-reference-detail-page.component.html',
  styleUrls: ['./park-reference-detail-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkReferenceDetailStateFacade],
  imports: [ParkReferenceDetailViewComponent]
})
export class ParkReferenceDetailPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly reference = this.stateFacade.reference;
  protected readonly attractionsLoading = this.stateFacade.attractionsLoading;
  protected readonly currentLang = signal<string>('en');
  protected readonly backLabelKey = signal<string>('parks.reference.backToParks');

  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly stateFacade: ParkReferenceDetailStateFacade,
    private readonly contextualBlockRefreshEvents: AdminContextualBlockRefreshEvents
  ) {
  }

  ngOnInit(): void {
    const initialLanguage: string = resolveLanguageFromActivatedRoute(this.route, this.translationService.getCurrentLang() || 'en');

    this.currentLang.set(initialLanguage);
    this.stateFacade.setCurrentLanguage(initialLanguage);

    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap) => {
      const id: string | null = params.get('id');
      const kind: ParkReferenceKind = this.resolveReferenceKind();

      this.backLabelKey.set(this.resolveBackLabelKey(kind));

      if (!id) {
        return;
      }

      this.stateFacade.loadReference(kind, id);
    });

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string) => {
      this.currentLang.set(language);
      this.stateFacade.setCurrentLanguage(language);
    });

    this.contextualBlockRefreshEvents.appliedBlock$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((event: AdminContextualBlockAppliedEvent) => {
      if (event.entityType !== 'AttractionManufacturer' || this.resolveReferenceKind() !== 'manufacturer') {
        return;
      }

      const currentReferenceId: string | null = this.reference()?.id ?? this.route.snapshot.paramMap.get('id');
      if (currentReferenceId !== event.entityId) {
        return;
      }

      this.stateFacade.loadReference('manufacturer', event.entityId);
    });
  }

  goBack(): void {
    const routeSegment: string = this.resolveBackRouteSegment(this.resolveReferenceKind());

    this.router.navigate(['/', this.currentLang(), routeSegment]);
  }

  onAttractionsPageChanged(event: { page?: number; rows?: number }): void {
    const page: number = (event.page ?? 0) + 1;
    const rows: number = event.rows ?? 12;
    this.stateFacade.loadManufacturerAttractionsPage(page, rows);
  }

  private resolveReferenceKind(): ParkReferenceKind {
    const kind: unknown = this.route.snapshot.data['referenceKind'];

    if (kind === 'founder' || kind === 'manufacturer') {
      return kind;
    }

    return 'operator';
  }

  private resolveBackRouteSegment(kind: ParkReferenceKind): string {
    if (kind === 'manufacturer') {
      return 'manufacturers';
    }

    return 'parks';
  }

  private resolveBackLabelKey(kind: ParkReferenceKind): string {
    if (kind === 'manufacturer') {
      return 'parks.reference.backToManufacturers';
    }

    return 'parks.reference.backToParks';
  }
}
