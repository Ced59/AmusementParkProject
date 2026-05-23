import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Card } from 'primeng/card';
import { InputText } from 'primeng/inputtext';
import { PrimeTemplate } from 'primeng/api';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';

import { AdminAuditLog, AdminAuditLogQuery, AdminAuditMetadataEntry } from '@app/models/admin/audit/admin-audit-log.models';
import { EmptyStateComponent } from '@app/components/shared/empty-state/empty-state.component';
import { PageStateComponent } from '@app/components/shared/page-state/page-state.component';
import { AdminAuditLogsStateFacade } from '@features/admin/audit-logs/state/admin-audit-logs-state.facade';

interface AdminAuditLogFiltersForm {
  fromUtc: FormControl<string>;
  toUtc: FormControl<string>;
  actorEmail: FormControl<string>;
  actorUserId: FormControl<string>;
  action: FormControl<string>;
  entityType: FormControl<string>;
  entityId: FormControl<string>;
  traceId: FormControl<string>;
}

@Component({
  selector: 'app-admin-audit-logs',
  templateUrl: './admin-audit-logs.component.html',
  styleUrls: ['./admin-audit-logs.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminAuditLogsStateFacade],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslateModule,
    ButtonDirective,
    Card,
    InputText,
    PrimeTemplate,
    TableModule,
    Tag,
    EmptyStateComponent,
    PageStateComponent
  ]
})
export class AdminAuditLogsComponent implements OnInit {
  protected readonly logs = this.stateFacade.logs;
  protected readonly state = this.stateFacade.state;
  protected readonly loading = this.stateFacade.loading;
  protected readonly totalRecords = this.stateFacade.totalRecords;
  protected readonly pageSize = this.stateFacade.pageSize;
  protected readonly currentPage = this.stateFacade.currentPage;
  protected readonly canShowHeaderTotal = computed(() => !this.loading());

  protected readonly filtersForm = new FormGroup<AdminAuditLogFiltersForm>({
    fromUtc: new FormControl<string>('', { nonNullable: true }),
    toUtc: new FormControl<string>('', { nonNullable: true }),
    actorEmail: new FormControl<string>('', { nonNullable: true }),
    actorUserId: new FormControl<string>('', { nonNullable: true }),
    action: new FormControl<string>('', { nonNullable: true }),
    entityType: new FormControl<string>('', { nonNullable: true }),
    entityId: new FormControl<string>('', { nonNullable: true }),
    traceId: new FormControl<string>('', { nonNullable: true })
  });

  constructor(private readonly stateFacade: AdminAuditLogsStateFacade) {
  }

  ngOnInit(): void {
    this.stateFacade.load({ page: this.currentPage(), size: this.pageSize() });
  }

  protected applyFilters(): void {
    this.stateFacade.load({
      ...this.toQueryFilters(),
      page: 1,
      size: this.pageSize()
    });
  }

  protected resetFilters(): void {
    this.filtersForm.reset();
    this.stateFacade.load({
      page: 1,
      size: this.pageSize(),
      fromUtc: null,
      toUtc: null,
      actorEmail: null,
      actorUserId: null,
      action: null,
      entityType: null,
      entityId: null,
      traceId: null
    });
  }

  protected onPageChanged(event: TableLazyLoadEvent): void {
    const rows: number = event.rows ?? this.pageSize();
    const first: number = event.first ?? 0;
    const page: number = Math.floor(first / rows) + 1;

    this.stateFacade.load({
      ...this.toQueryFilters(),
      page,
      size: rows
    });
  }

  protected trackByLogId(_: number, log: AdminAuditLog): string {
    return log.id;
  }

  protected statusSeverity(statusCode: number): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
    if (statusCode >= 200 && statusCode < 300) {
      return 'success';
    }

    if (statusCode >= 300 && statusCode < 400) {
      return 'info';
    }

    if (statusCode >= 400 && statusCode < 500) {
      return 'warn';
    }

    return 'danger';
  }

  protected metadataEntries(log: AdminAuditLog): AdminAuditMetadataEntry[] {
    return Object.entries(log.metadata ?? {})
      .sort(([leftKey], [rightKey]) => leftKey.localeCompare(rightKey))
      .map(([key, value]) => ({ key, value }));
  }

  protected hasMetadata(log: AdminAuditLog): boolean {
    return this.metadataEntries(log).length > 0;
  }

  private toQueryFilters(): Partial<AdminAuditLogQuery> {
    const value = this.filtersForm.getRawValue();
    return {
      fromUtc: this.toUtcIso(value.fromUtc),
      toUtc: this.toUtcIso(value.toUtc),
      actorEmail: this.normalize(value.actorEmail),
      actorUserId: this.normalize(value.actorUserId),
      action: this.normalize(value.action),
      entityType: this.normalize(value.entityType),
      entityId: this.normalize(value.entityId),
      traceId: this.normalize(value.traceId)
    };
  }

  private toUtcIso(value: string): string | null {
    const normalizedValue: string | null = this.normalize(value);
    if (!normalizedValue) {
      return null;
    }

    const date: Date = new Date(normalizedValue);
    if (Number.isNaN(date.getTime())) {
      return null;
    }

    return date.toISOString();
  }

  private normalize(value: string | null | undefined): string | null {
    if (!value || value.trim().length === 0) {
      return null;
    }

    return value.trim();
  }
}
