import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { NgFor } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { InputText } from 'primeng/inputtext';

import { PageStateComponent } from '@app/components/shared/page-state/page-state.component';
import { PaginationComponent } from '@app/components/shared/pagination/pagination.component';
import { ParkCardComponent } from '@app/components/public/park-card/park-card.component';
import { PaginationContract } from '@shared/models/contracts';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';

@Component({
  selector: 'app-park-list-view',
  templateUrl: './park-list-view.component.html',
  styleUrls: ['./park-list-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [InputText, PageStateComponent, PaginationComponent, NgFor, ParkCardComponent, TranslateModule, UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective]
})
export class ParkListViewComponent {
  @Input() state!: Signal<ScreenState<unknown, string>>;
  @Input() parks!: Signal<ParkCardModel[]>;
  @Input() pagination!: Signal<PaginationContract | null>;
  @Input() currentLang!: Signal<string>;
  @Input() searchTerm!: Signal<string>;

  @Output() searchInputChanged: EventEmitter<string> = new EventEmitter<string>();
  @Output() clearSearchClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() pageChanged: EventEmitter<{ page?: number; rows?: number }> = new EventEmitter<{ page?: number; rows?: number }>();

  onSearchInput(value: string): void {
    this.searchInputChanged.emit(value);
  }

  clearSearch(): void {
    this.clearSearchClicked.emit();
  }

  onPageChange(event: { page?: number; rows?: number }): void {
    this.pageChanged.emit(event);
  }
}
