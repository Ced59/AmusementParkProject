import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';

import { ButtonDirective } from '@shared/ui/primitives/button';
import { Card } from '@shared/ui/primitives/card';
import { TableModule } from '@shared/ui/primitives/table';
import { Tag } from '@shared/ui/primitives/tag';

import { DataSourceSummary } from '@app/models/admin/data/data-management.models';

@Component({
  selector: 'app-admin-data-sources-list',
  standalone: true,
  imports: [CommonModule, ButtonDirective, Card, TableModule, Tag],
  templateUrl: './admin-data-sources-list.component.html',
  styleUrl: './admin-data-sources-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminDataSourcesListComponent {
  @Input({ required: true })
  public sources: DataSourceSummary[] = [];

  @Output()
  public readonly manageSource = new EventEmitter<string>();

  protected getSeverity(source: DataSourceSummary): 'success' | 'secondary' {
    return source.lastImportUtc ? 'success' : 'secondary';
  }
}
