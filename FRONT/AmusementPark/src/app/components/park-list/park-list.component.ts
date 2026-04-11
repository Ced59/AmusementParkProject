import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Subject, Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';

import { Park } from '../../models/parks/park';
import { Pagination } from '../../models/shared/pagination';
import { ViewState } from '../../models/shared/view-state';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { TranslationService } from '../../services/translation.service';
import { Bind } from 'primeng/bind';
import { InputText } from 'primeng/inputtext';
import { ButtonDirective } from 'primeng/button';
import { PageStateComponent } from '../shared/page-state/page-state.component';
import { NgFor, NgIf } from '@angular/common';
import { ParkCardComponent } from '../public/park-card/park-card.component';
import { Paginator } from 'primeng/paginator';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-park-list',
    templateUrl: './park-list.component.html',
    styleUrls: ['./park-list.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [Bind, InputText, ButtonDirective, PageStateComponent, NgFor, ParkCardComponent, NgIf, Paginator, TranslateModule]
})
export class ParkListComponent implements OnInit, OnDestroy {
  parks: Park[] = [];
  pagination: Pagination | null = null;
  pageState: ViewState = ViewState.Loading;

  currentLang: string = 'en';
  searchTerm: string = '';
  pageSize: number = 9;

  private readonly searchSubject: Subject<string> = new Subject<string>();
  private readonly subscriptions: Subscription = new Subscription();

  constructor(
    private readonly parksApiService: ParksApiService,
    private readonly translationService: TranslationService,
    private readonly cdr: ChangeDetectorRef
  ) {
  }

  ngOnInit(): void {
    this.currentLang = this.translationService.getCurrentLang() || 'en';

    this.subscriptions.add(
      this.searchSubject.pipe(debounceTime(300)).subscribe((term: string) => {
        this.loadParks(1, this.pageSize, term);
      })
    );

    this.loadParks(1, this.pageSize, this.searchTerm);
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  onSearchInput(value: string): void {
    this.searchTerm = value.trim();
    this.searchSubject.next(this.searchTerm);
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.searchSubject.next(this.searchTerm);
  }

  onPageChange(event: { page?: number; rows?: number }): void {
    const page: number = (event.page ?? 0) + 1;
    const rows: number = event.rows ?? this.pageSize;
    this.pageSize = rows;
    this.loadParks(page, rows, this.searchTerm);
  }

  private loadParks(page: number, size: number, term: string): void {
    this.pageState = ViewState.Loading;
    this.cdr.markForCheck();

    const request$ = term
      ? this.parksApiService.searchParks(term, page, size)
      : this.parksApiService.getParksPaginated(page, size);

    this.subscriptions.add(
      request$.subscribe({
        next: (response) => {
          this.parks = response.data ?? [];
          this.pagination = response.pagination ?? null;
          this.pageState = this.parks.length > 0 ? ViewState.Ready : ViewState.Empty;
          this.cdr.markForCheck();
        },
        error: (error: unknown) => {
          console.error('Error fetching parks:', error);
          this.pageState = ViewState.Error;
          this.cdr.markForCheck();
        }
      })
    );
  }
}
