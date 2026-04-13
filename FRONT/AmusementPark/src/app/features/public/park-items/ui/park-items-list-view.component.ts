import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal, computed } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';

import { EmptyStateComponent } from '@app/components/shared/empty-state/empty-state.component';
import { PageStateComponent } from '@app/components/shared/page-state/page-state.component';
import { PaginationComponent } from '@app/components/shared/pagination/pagination.component';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { ParkItemCardViewModel } from '../models/park-item-card.model';
import { SelectOption } from '../models/select-option.model';
import { ParkItemsPageViewModel, ParkItemZoneCardViewModel } from '../models/park-items-page-view.model';
import { ParkItemCardComponent } from './park-item-card.component';
import { ParkItemsFiltersComponent } from './park-items-filters.component';
import { ParkItemsZoneListComponent } from './park-items-zone-list.component';

@Component({
  selector: 'app-park-items-list-view',
  templateUrl: './park-items-list-view.component.html',
  styleUrls: ['./park-items-list-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NgIf,
    NgFor,
    TranslateModule,
    ButtonDirective,
    EmptyStateComponent,
    PageStateComponent,
    PaginationComponent,
    ParkItemCardComponent,
    ParkItemsFiltersComponent,
    ParkItemsZoneListComponent
  ]
})
export class ParkItemsListViewComponent {
  @Input({ required: true }) state!: Signal<ScreenState<unknown, string>>;
  @Input({ required: true }) pageView!: Signal<ParkItemsPageViewModel | null>;
  @Input({ required: true }) zoneCards!: Signal<ParkItemZoneCardViewModel[]>;
  @Input({ required: true }) pagedItems!: Signal<ParkItemCardViewModel[]>;
  @Input({ required: true }) searchTerm!: Signal<string>;
  @Input({ required: true }) selectedCategory!: Signal<string | null>;
  @Input({ required: true }) selectedType!: Signal<string | null>;
  @Input({ required: true }) selectedZoneId!: Signal<string | null>;
  @Input({ required: true }) categoryOptions!: Signal<SelectOption[]>;
  @Input({ required: true }) typeOptions!: Signal<SelectOption[]>;
  @Input({ required: true }) zoneOptions!: Signal<SelectOption[]>;
  @Input({ required: true }) totalResults!: Signal<number>;
  @Input({ required: true }) rangeStart!: Signal<number>;
  @Input({ required: true }) rangeEnd!: Signal<number>;
  @Input({ required: true }) currentPage!: Signal<number>;
  @Input({ required: true }) pageSize!: Signal<number>;

  @Output() backClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() clearFiltersClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() searchChanged: EventEmitter<string> = new EventEmitter<string>();
  @Output() categoryChanged: EventEmitter<string | null> = new EventEmitter<string | null>();
  @Output() typeChanged: EventEmitter<string | null> = new EventEmitter<string | null>();
  @Output() zoneChanged: EventEmitter<string | null> = new EventEmitter<string | null>();
  @Output() zoneSelected: EventEmitter<string | null> = new EventEmitter<string | null>();
  @Output() pageChanged: EventEmitter<{ page?: number; rows?: number }> = new EventEmitter<{ page?: number; rows?: number }>();

  protected readonly hasZones = computed(() => this.zoneCards().length > 0);

  onBackClicked(): void {
    this.backClicked.emit();
  }

  clearFilters(): void {
    this.clearFiltersClicked.emit();
  }

  onSearchChanged(value: string): void {
    this.searchChanged.emit(value);
  }

  onCategoryChanged(value: string | null): void {
    this.categoryChanged.emit(value);
  }

  onTypeChanged(value: string | null): void {
    this.typeChanged.emit(value);
  }

  onZoneChanged(value: string | null): void {
    this.zoneChanged.emit(value);
  }

  onZoneSelected(zoneId: string | null): void {
    this.zoneSelected.emit(zoneId);
  }

  onPageChanged(event: { page?: number; rows?: number }): void {
    this.pageChanged.emit(event);
  }
}
