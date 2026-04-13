import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NgFor, NgIf } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { Bind } from 'primeng/bind';
import { ButtonDirective } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { SearchResultItem } from '@app/models/search/search-result-item';
import { Park } from '@app/models/parks/park';
import { PaginationContract } from '@shared/models/contracts';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { PageStateComponent } from '../shared/page-state/page-state.component';
import { PaginationComponent } from '../shared/pagination/pagination.component';
import { EmptyStateComponent } from '../shared/empty-state/empty-state.component';
import { ParkCardComponent } from '../public/park-card/park-card.component';
import { SearchResultCardComponent } from '../public/search-result-card/search-result-card.component';

@Component({
  selector: 'app-home-view',
  templateUrl: './home-view.component.html',
  styleUrls: ['./home.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Bind, ButtonDirective, RouterLink, InputText, Select, FormsModule, PageStateComponent, PaginationComponent, EmptyStateComponent, NgFor, ParkCardComponent, NgIf, SearchResultCardComponent, TranslateModule]
})
export class HomeViewComponent {
  @Input() currentLang!: Signal<string>;
  @Input() searchTerm!: Signal<string>;
  @Input() selectedCategory!: Signal<string>;
  @Input() categoryOptions: { labelKey: string; value: string }[] = [];
  @Input() featuredState!: Signal<ScreenState<unknown, string>>;
  @Input() featuredParks!: Signal<Park[]>;
  @Input() searchState!: Signal<ScreenState<unknown, string>>;
  @Input() results!: Signal<SearchResultItem[]>;
  @Input() pagination!: Signal<PaginationContract | null>;
  @Input() hasPerformedSearch!: Signal<boolean>;
  @Input() searchResultsTotal: number = 0;
  @Input() searchResultsHintKey: string = '';

  @Output() searchInputChanged: EventEmitter<string> = new EventEmitter<string>();
  @Output() categoryChanged: EventEmitter<string> = new EventEmitter<string>();
  @Output() pageChanged: EventEmitter<{ page?: number; rows?: number }> = new EventEmitter<{ page?: number; rows?: number }>();

  onSearchInput(value: string): void {
    this.searchInputChanged.emit(value);
  }

  onCategoryChange(value: string): void {
    this.categoryChanged.emit(value);
  }

  onPageChange(event: { page?: number; rows?: number }): void {
    this.pageChanged.emit(event);
  }
}
