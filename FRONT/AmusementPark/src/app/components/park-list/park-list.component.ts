import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { Subject } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { TranslationService } from '../../services/translation.service';
import { ParkListStateFacade } from '@features/public/parks/state/park-list-state.facade';
import { ParkListViewComponent } from './park-list-view.component';

@Component({
  selector: 'app-park-list',
  templateUrl: './park-list.component.html',
  styleUrls: ['./park-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkListStateFacade],
  imports: [ParkListViewComponent]
})
export class ParkListComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly parks = this.stateFacade.parks;
  protected readonly pagination = this.stateFacade.pagination;
  protected readonly currentLang = signal<string>('en');
  protected readonly searchTerm = signal<string>('');

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly searchSubject: Subject<string> = new Subject<string>();

  constructor(
    private readonly stateFacade: ParkListStateFacade,
    private readonly translationService: TranslationService
  ) {
  }

  ngOnInit(): void {
    this.currentLang.set(this.translationService.getCurrentLang() || 'en');

    this.searchSubject.pipe(
      debounceTime(300),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((term: string) => {
      this.stateFacade.loadParks(1, this.stateFacade.pageSize(), term);
    });

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
}
