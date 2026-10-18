import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Table, TableLazyLoadEvent, TableSortEvent } from '@shared/ui/primitives/table';
import { SortableColumn } from '@shared/ui/primitives/sortable-column';
import { UiTemplate } from '@shared/ui/primitives/ui-template';

// Isolated presentation fixture: never imported by application code.
@Component({
  selector: 'app-sortable-table-fixture',
  imports: [Table, SortableColumn, UiTemplate],
  template: `<app-ui-table [value]="rows" [first]="20"
      (onSort)="sortEvents.push($event)" (onLazyLoad)="lazyEvents.push($event)">
    <ng-template appUiTemplate="header"><tr>
      <th appUiSortableColumn="name">Name</th><th>Plain column</th>
    </tr></ng-template>
    <ng-template appUiTemplate="body" let-row><tr><td>{{ row.name }}</td><td>Value</td></tr></ng-template>
  </app-ui-table>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SortableTableFixtureComponent {
  readonly rows = [{ name: 'Alpha' }, { name: 'Beta' }];
  readonly sortEvents: TableSortEvent[] = [];
  readonly lazyEvents: TableLazyLoadEvent[] = [];
}
