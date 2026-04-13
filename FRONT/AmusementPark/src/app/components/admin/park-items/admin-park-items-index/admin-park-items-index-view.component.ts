import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { PrimeTemplate } from 'primeng/api';
import { Tag } from 'primeng/tag';
import { ButtonDirective } from 'primeng/button';
import { PageStateComponent } from '../../../shared/page-state/page-state.component';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { EmptyStateComponent } from '../../../shared/empty-state/empty-state.component';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { ScreenState } from '@shared/models/contracts/screen-state.model';

@Component({
  selector: 'app-admin-park-items-index-view',
  templateUrl: './admin-park-items-index-view.component.html',
  styleUrls: ['./admin-park-items-index.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Bind, Card, FormsModule, InputText, Select, TableModule, PrimeTemplate, Tag, ButtonDirective, TranslateModule, PageStateComponent, PaginationComponent, EmptyStateComponent]
})
export class AdminParkItemsIndexViewComponent {
  @Input() state!: Signal<ScreenState<unknown, string>>;
  @Input() rows!: Signal<ParkItemAdminRow[]>;
  @Input() parkOptions!: Signal<Array<{ label: string; value: string | null }>>;
  @Input() totalRecords!: Signal<number>;
  @Input() selectedParkId: string | null = null;
  @Input() searchTerm: string = '';
  @Input() pageSize: number = 20;
  @Input() getTypeLabelKeyFn: (itemType: string | number | null | undefined) => string = () => 'parkExplorer.types.other';

  @Output() filtersChanged: EventEmitter<{ selectedParkId: string | null; searchTerm: string }> = new EventEmitter<{ selectedParkId: string | null; searchTerm: string }>();
  @Output() pageChanged: EventEmitter<{ page?: number; rows?: number }> = new EventEmitter<{ page?: number; rows?: number }>();
  @Output() editClicked: EventEmitter<ParkItemAdminRow> = new EventEmitter<ParkItemAdminRow>();

  onSelectedParkIdChange(value: string | null): void {
    this.selectedParkId = value;
    this.emitFiltersChanged();
  }

  onSearchTermChanged(value: string): void {
    this.searchTerm = value;
    this.emitFiltersChanged();
  }

  onPageChange(event: { page?: number; rows?: number }): void {
    this.pageChanged.emit(event);
  }

  getTypeLabelKey(itemType: string | number | null | undefined): string {
    return this.getTypeLabelKeyFn(itemType);
  }

  goToEdit(row: ParkItemAdminRow): void {
    this.editClicked.emit(row);
  }

  private emitFiltersChanged(): void {
    this.filtersChanged.emit({
      selectedParkId: this.selectedParkId,
      searchTerm: this.searchTerm
    });
  }
}
