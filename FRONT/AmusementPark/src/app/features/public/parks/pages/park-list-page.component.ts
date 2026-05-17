import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { TranslationService } from '@app/services/translation.service';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { ParkListStateFacade } from '../state/park-list-state.facade';
import { ParkListViewComponent } from '../ui/park-list-view.component';

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
  protected readonly currentLang = signal<string>('en');
  protected readonly searchTerm = signal<string>('');

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly searchSubject: Subject<string> = new Subject<string>();

  constructor(
    private readonly route: ActivatedRoute,
    private readonly stateFacade: ParkListStateFacade,
    private readonly translationService: TranslationService
  ) {
  }

  ngOnInit(): void {
    const initialLanguage: string = this.route.parent?.snapshot.paramMap.get('lang')
      ?? this.translationService.getCurrentLang()
      ?? 'en';

    this.currentLang.set(initialLanguage);
    this.stateFacade.setCurrentLanguage(initialLanguage);

    if (this.route.parent) {
      this.route.parent.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
        const language: string = params.get('lang') ?? 'en';
        this.currentLang.set(language);
        this.stateFacade.setCurrentLanguage(language);
      });
    }

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string) => {
      this.currentLang.set(language);
      this.stateFacade.setCurrentLanguage(language);
    });

    this.searchSubject.pipe(
      debounceTime(300),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((term: string) => {
      this.stateFacade.clearSelectedPark();
      this.stateFacade.loadVisibleMapPoints(term);
      this.stateFacade.loadParks(1, this.stateFacade.pageSize(), term);
    });

    this.stateFacade.loadVisibleMapPoints(this.searchTerm());
    this.stateFacade.loadParks(1, this.stateFacade.pageSize(), this.searchTerm());
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
    this.stateFacade.loadParks(page, rows, this.searchTerm());
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
