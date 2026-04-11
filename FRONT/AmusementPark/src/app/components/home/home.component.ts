import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { EMPTY, Subject } from 'rxjs';
import { debounceTime, switchMap } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { TranslationService } from '../../services/translation.service';
import { Bind } from 'primeng/bind';
import { ButtonDirective } from 'primeng/button';
import { RouterLink } from '@angular/router';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { FormsModule } from '@angular/forms';
import { PageStateComponent } from '../shared/page-state/page-state.component';
import { NgFor, NgIf } from '@angular/common';
import { ParkCardComponent } from '../public/park-card/park-card.component';
import { SearchResultCardComponent } from '../public/search-result-card/search-result-card.component';
import { Paginator } from 'primeng/paginator';
import { TranslateModule } from '@ngx-translate/core';
import { HomeStateFacade } from '@features/public/home/state/home-state.facade';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [HomeStateFacade],
    imports: [Bind, ButtonDirective, RouterLink, InputText, Select, FormsModule, PageStateComponent, NgFor, ParkCardComponent, NgIf, SearchResultCardComponent, Paginator, TranslateModule]
})
export class HomeComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');
  protected readonly searchTerm = signal<string>('');
  protected readonly selectedCategory = signal<string>('');

  protected readonly categoryOptions: { labelKey: string; value: string }[] = [
    { labelKey: 'home.categories.everywhere', value: '' },
    { labelKey: 'home.categories.park', value: 'park' },
    { labelKey: 'home.categories.parkItems', value: 'parkItems' },
    { labelKey: 'home.categories.operators', value: 'operators' },
    { labelKey: 'home.categories.manufacturers', value: 'manufacturers' }
  ];

  protected readonly featuredState = this.stateFacade.featuredState;
  protected readonly featuredParks = this.stateFacade.featuredParks;
  protected readonly searchState = this.stateFacade.searchState;
  protected readonly results = this.stateFacade.searchResults;
  protected readonly pagination = this.stateFacade.searchPagination;
  protected readonly hasPerformedSearch = this.stateFacade.hasPerformedSearch;

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly searchSubject: Subject<void> = new Subject<void>();

  constructor(
    private readonly stateFacade: HomeStateFacade,
    private readonly translationService: TranslationService
  ) {
  }

  ngOnInit(): void {
    this.currentLang.set(this.translationService.getCurrentLang() || 'en');
    this.stateFacade.loadFeaturedParks();

    this.searchSubject.pipe(
      debounceTime(300),
      switchMap(() => {
        this.stateFacade.search(this.searchTerm(), this.selectedCategory(), 1, this.stateFacade.pageSize());
        return EMPTY;
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe();
  }

  onSearchInput(value: string): void {
    this.searchTerm.set(value.trim());
    this.searchSubject.next();
  }

  onCategoryChange(value: string): void {
    this.selectedCategory.set(value ?? '');
    this.searchSubject.next();
  }

  get searchResultsTotal(): number {
    return this.pagination()?.totalItems ?? this.results().length;
  }

  get searchResultsHintKey(): string {
    return this.hasPerformedSearch() ? 'home.search.resultsSubtitle' : 'home.search.hintMessage';
  }

  onPageChange(event: { page?: number; rows?: number }): void {
    const page: number = (event.page ?? 0) + 1;
    const rows: number = event.rows ?? this.stateFacade.pageSize();
    this.stateFacade.search(this.searchTerm(), this.selectedCategory(), page, rows);
  }
}
