import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { UiButtonDirective } from '@ui/primitives';
import {
  PassportStatisticsNavigationViewModel,
  PassportStatisticsTableCellViewModel,
  PassportStatisticsTableColumnViewModel,
  PassportStatisticsTableViewModel
} from '../../models/passport-statistics-view.models';

@Component({
  selector: 'app-passport-table',
  templateUrl: './passport-table.component.html',
  styleUrl: './passport-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule, UiButtonDirective]
})
export class PassportTableComponent {
  @Input({ required: true }) table!: PassportStatisticsTableViewModel;
  @Output() navigationSelected: EventEmitter<PassportStatisticsNavigationViewModel> =
    new EventEmitter<PassportStatisticsNavigationViewModel>();

  protected cellFor(
    cells: PassportStatisticsTableCellViewModel[],
    column: PassportStatisticsTableColumnViewModel
  ): PassportStatisticsTableCellViewModel | null {
    return cells.find((cell) => cell.columnKey === column.key) ?? null;
  }
}
