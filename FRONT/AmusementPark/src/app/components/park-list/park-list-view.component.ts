import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { Bind } from 'primeng/bind';
import { InputText } from 'primeng/inputtext';
import { ButtonDirective } from 'primeng/button';
import { Paginator } from 'primeng/paginator';
import { TranslateModule } from '@ngx-translate/core';
import { Park } from '../../models/parks/park';
import { PaginationContract } from '@shared/models/contracts';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { PageStateComponent } from '../shared/page-state/page-state.component';
import { ParkCardComponent } from '../public/park-card/park-card.component';

@Component({
  selector: 'app-park-list-view',
  templateUrl: './park-list-view.component.html',
  styleUrls: ['./park-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Bind, InputText, ButtonDirective, PageStateComponent, NgFor, ParkCardComponent, NgIf, Paginator, TranslateModule]
})
export class ParkListViewComponent {
  @Input() state!: Signal<ScreenState<unknown, string>>;
  @Input() parks!: Signal<Park[]>;
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
