import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { TranslationService } from '@app/services/translation.service';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { ParkRegionFilter } from '@shared/models/geo/world-region-filter.model';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { ParkListStateFacade } from '../state/park-list-state.facade';
import { ParkListViewComponent } from '../ui/park-list-view.component';
import { SeoService } from '@core/seo/seo.service';

@Component({
  selector: 'app-park-list-page',
  templateUrl: './park-list-page.component.html',
  styleUrls: ['./park-list-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkListStateFacade],
  imports: [ParkListViewComponent]
})
export class ParkListPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly mapState = this.stateFacade.mapState;
  protected readonly parks = this.stateFacade.parks;
  protected readonly displayedParks = this.stateFacade.displayedParks;
  protected readonly pagination = this.stateFacade.pagination;
  protected readonly visibleMapPoints = this.stateFacade.visibleMapPoints;
  protected readonly visibleCountryCount = this.stateFacade.visibleCountryCount;
  protected readonly selectedMapParkId = this.stateFacade.selectedParkId;
  protected readonly selectedParkCard = this.stateFacade.selectedParkCard;
  protected readonly selectedRegion = this.stateFacade.selectedRegion;
  protected readonly currentLang = signal<string>('en');
  protected readonly searchTerm = signal<string>('');

  private readonly searchSubject: Subject<string> = new Subject<string>();

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly stateFacade: ParkListStateFacade,
    private readonly translationService: TranslationService,
    private readonly seoService: SeoService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    const initialLanguage: string = resolveLanguageFromActivatedRoute(this.route, this.translationService.getCurrentLang() || 'en');

    this.currentLang.set(initialLanguage);
    this.stateFacade.setCurrentLanguage(initialLanguage);
    this.seoService.applyParkListSeo(initialLanguage, this.router.url);


    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string) => {
      this.currentLang.set(language);
      this.stateFacade.setCurrentLanguage(language);
      this.seoService.applyParkListSeo(language, this.router.url);
      this.stateFacade.loadVisibleMapPoints(this.searchTerm(), this.selectedRegion());
    });

    this.searchSubject.pipe(
      debounceTime(300),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((term: string) => {
      this.stateFacade.clearSelectedPark();
      this.stateFacade.loadVisibleMapPoints(term, this.selectedRegion());
      this.stateFacade.loadParks(1, this.stateFacade.pageSize(), term, this.selectedRegion());
    });

    this.stateFacade.loadVisibleMapPoints(this.searchTerm(), this.selectedRegion());
    this.stateFacade.loadParks(1, this.stateFacade.pageSize(), this.searchTerm(), this.selectedRegion());
  }

  onSearchInput(value: string): void {
    const normalizedValue: string = value.trim();
    this.searchTerm.set(normalizedValue);
    this.searchSubject.next(normalizedValue);
  }

  clearSearch(): void {
    this.searchTerm.set('');
    this.searchSubject.next('');
  }

  onPageChange(event: { page?: number; rows?: number }): void {
    const page: number = (event.page ?? 0) + 1;
    const rows: number = event.rows ?? this.stateFacade.pageSize();
    this.stateFacade.loadParks(page, rows, this.searchTerm(), this.selectedRegion());
  }

  onRegionFilterChanged(region: ParkRegionFilter | null): void {
    this.stateFacade.setSelectedRegion(region);
    this.stateFacade.clearSelectedPark();
    this.stateFacade.loadVisibleMapPoints(this.searchTerm(), region);
    this.stateFacade.loadParks(1, this.stateFacade.pageSize(), this.searchTerm(), region);
  }

  onMapParkSelected(parkId: string | null): void {
    this.stateFacade.selectParkFromMap(parkId);
  }

  onResultParkFocused(park: ParkCardModel): void {
    this.stateFacade.selectParkFromCard(park);
  }

  clearSelectedPark(): void {
    this.stateFacade.clearSelectedPark();
  }
}
