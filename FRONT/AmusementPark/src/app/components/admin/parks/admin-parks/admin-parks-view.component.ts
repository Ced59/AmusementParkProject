import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { PrimeTemplate } from 'primeng/api';
import { InputText } from 'primeng/inputtext';
import { ButtonDirective } from 'primeng/button';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { TranslateModule } from '@ngx-translate/core';
import { EmptyStateComponent } from '../../../shared/empty-state/empty-state.component';
import { Park } from '../../../../models/parks/park';

@Component({
  selector: 'app-admin-parks-view',
  templateUrl: './admin-parks-view.component.html',
  styleUrls: ['./admin-parks.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Bind, Card, PrimeTemplate, FormsModule, InputText, ButtonDirective, TableModule, ToggleSwitch, RouterLink, TranslateModule, EmptyStateComponent]
})
export class AdminParksViewComponent {
  @Input() parks!: Signal<Park[]>;
  @Input() loading!: Signal<boolean>;
  @Input() totalRecords!: Signal<number>;
  @Input() pageSize!: Signal<number>;
  @Input() currentPage!: Signal<number>;
  @Input() searchQuery!: Signal<string>;
  @Input() canShowHeaderTotal!: Signal<boolean>;
  @Input() canClearSearch!: Signal<boolean>;
  @Input() getTypeTranslationKeyFn: (type: string | null | undefined) => string = () => 'admin.parks.types.notSpecified';

  @Output() searchQueryChanged: EventEmitter<string> = new EventEmitter<string>();
  @Output() searchClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() clearSearchClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() pageChanged: EventEmitter<TableLazyLoadEvent> = new EventEmitter<TableLazyLoadEvent>();
  @Output() visibilityChanged: EventEmitter<Park> = new EventEmitter<Park>();

  onSearchQueryChanged(searchQuery: string): void {
    this.searchQueryChanged.emit(searchQuery);
  }

  onSearch(): void {
    this.searchClicked.emit();
  }

  clearSearch(): void {
    this.clearSearchClicked.emit();
  }

  onPageChanged(event: TableLazyLoadEvent): void {
    this.pageChanged.emit(event);
  }

  onVisibilityChange(park: Park): void {
    this.visibilityChanged.emit(park);
  }

  getTypeTranslationKey(type: string | null | undefined): string {
    return this.getTypeTranslationKeyFn(type);
  }
}
