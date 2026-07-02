import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { Card } from '@shared/ui/primitives/card';
import { InputText } from '@shared/ui/primitives/inputtext';
import { UiTemplate } from '@shared/ui/primitives/api';
import { TableLazyLoadEvent, TableModule } from '@shared/ui/primitives/table';
import { Tag } from '@shared/ui/primitives/tag';

import { AdminContactGrievance, AdminContactGrievanceQuery } from '@app/models/contact/contact-grievance.models';
import { EmptyStateComponent } from '@shared/components/empty-state/empty-state.component';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { ScrollAnchorService } from '@shared/services/scroll/scroll-anchor.service';
import { AdminContactGrievancesFacade } from '@features/admin/contact/state/admin-contact-grievances.facade';

interface AdminContactGrievanceFiltersForm {
  readonly search: FormControl<string>;
  readonly ipAddress: FormControl<string>;
  readonly languageCode: FormControl<string>;
}

@Component({
  selector: 'app-admin-contact-grievances',
  templateUrl: './admin-contact-grievances.component.html',
  styleUrl: './admin-contact-grievances.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminContactGrievancesFacade],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslateModule,
    ButtonDirective,
    Card,
    InputText,
    UiTemplate,
    TableModule,
    Tag,
    EmptyStateComponent,
    PageStateComponent
  ]
})
export class AdminContactGrievancesComponent implements OnInit {
  protected readonly grievances = this.facade.grievances;
  protected readonly state = this.facade.state;
  protected readonly loading = this.facade.loading;
  protected readonly totalRecords = this.facade.totalRecords;
  protected readonly pageSize = this.facade.pageSize;
  protected readonly currentPage = this.facade.currentPage;
  protected readonly canShowHeaderTotal = computed(() => !this.loading());
  protected readonly filtersForm = new FormGroup<AdminContactGrievanceFiltersForm>({
    search: new FormControl<string>('', { nonNullable: true }),
    ipAddress: new FormControl<string>('', { nonNullable: true }),
    languageCode: new FormControl<string>('', { nonNullable: true })
  });

  constructor(
    private readonly facade: AdminContactGrievancesFacade,
    private readonly scrollAnchorService: ScrollAnchorService
  ) {
  }

  ngOnInit(): void {
    this.facade.load({ page: this.currentPage(), size: this.pageSize() });
  }

  protected applyFilters(): void {
    this.facade.load({
      ...this.toQueryFilters(),
      page: 1,
      size: this.pageSize()
    });
  }

  protected resetFilters(): void {
    this.filtersForm.reset();
    this.facade.load({
      page: 1,
      size: this.pageSize(),
      search: null,
      ipAddress: null,
      languageCode: null
    });
  }

  protected onPageChanged(event: TableLazyLoadEvent): void {
    const rows: number = event.rows ?? this.pageSize();
    const first: number = event.first ?? 0;
    const page: number = Math.floor(first / rows) + 1;
    const shouldScroll: boolean = page !== this.currentPage() || rows !== this.pageSize();

    this.facade.load({
      ...this.toQueryFilters(),
      page,
      size: rows
    });

    if (shouldScroll) {
      this.scrollAnchorService.scrollToSelector('[data-pagination-scroll-target="admin-contact-grievances"]');
    }
  }

  protected trackByGrievanceId(_: number, grievance: AdminContactGrievance): string {
    return grievance.id;
  }

  private toQueryFilters(): Partial<AdminContactGrievanceQuery> {
    const value = this.filtersForm.getRawValue();
    return {
      search: this.normalize(value.search),
      ipAddress: this.normalize(value.ipAddress),
      languageCode: this.normalize(value.languageCode)
    };
  }

  private normalize(value: string | null | undefined): string | null {
    if (!value || value.trim().length === 0) {
      return null;
    }

    return value.trim();
  }
}
