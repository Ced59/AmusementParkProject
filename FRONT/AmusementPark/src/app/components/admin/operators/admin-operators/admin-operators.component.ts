import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { PrimeTemplate } from 'primeng/api';
import { FormsModule } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { ButtonDirective } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { PaginatorState } from 'primeng/paginator';
import { TranslateModule } from '@ngx-translate/core';
import { AdminReviewStatus, getAdminReviewStatusSeverity, getAdminReviewStatusTranslationKey } from '@app/models/admin/admin-review-status';
import { ParkOperator } from '@app/models/parks/park-operator';
import { EmptyStateComponent } from '../../../shared/empty-state/empty-state.component';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { AdminOperatorsStateFacade } from '@features/admin/operators/state/admin-operators-state.facade';

@Component({
  selector: 'app-admin-operators',
  templateUrl: './admin-operators.component.html',
  styleUrls: ['./admin-operators.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminOperatorsStateFacade],
  imports: [Bind, Card, PrimeTemplate, FormsModule, InputText, ButtonDirective, RouterLink, TableModule, Tag, TranslateModule, EmptyStateComponent, PaginationComponent]
})
export class AdminOperatorsComponent implements OnInit {
  protected readonly operators = this.stateFacade.pagedOperators;
  protected readonly loading = this.stateFacade.loading;
  protected readonly totalCount = this.stateFacade.totalCount;
  protected readonly currentPage = this.stateFacade.currentPage;
  protected readonly pageSize = this.stateFacade.pageSize;
  protected readonly selectedOperatorIds = this.stateFacade.selectedOperatorIds;
  protected readonly selectedCount = this.stateFacade.selectedCount;
  currentLang: string = 'en';

  constructor(
    protected readonly stateFacade: AdminOperatorsStateFacade,
    private readonly route: ActivatedRoute
  ) {
  }

  ngOnInit(): void {
    this.currentLang =
      this.route.root.firstChild?.snapshot.params['lang'] ??
      this.route.snapshot.params['lang'] ??
      'en';

    this.stateFacade.loadOperators();
  }

  onSearchQueryChanged(searchQuery: string): void {
    this.stateFacade.setSearchQuery(searchQuery);
  }

  onAdminReviewStatusChanged(adminReviewStatus: AdminReviewStatus | null): void {
    this.stateFacade.setAdminReviewStatusFilter(adminReviewStatus);
  }

  onPageChanged(event: PaginatorState): void {
    const pageSize: number = event.rows ?? this.pageSize();
    const first: number = event.first ?? 0;
    const page: number = Math.floor(first / pageSize) + 1;
    this.stateFacade.setPage(page, pageSize);
  }

  isOperatorSelected(parkOperator: ParkOperator): boolean {
    return !!parkOperator.id && this.selectedOperatorIds().includes(parkOperator.id);
  }

  areAllCurrentOperatorsSelected(): boolean {
    const ids: string[] = this.operators().map((operator: ParkOperator) => operator.id).filter((id: string | undefined): id is string => !!id);
    return ids.length > 0 && ids.every((id: string) => this.selectedOperatorIds().includes(id));
  }

  onOperatorSelectionChange(parkOperator: ParkOperator, event: Event): void {
    if (!parkOperator.id) {
      return;
    }

    this.stateFacade.setOperatorSelection(parkOperator.id, (event.target as HTMLInputElement).checked);
  }

  onAllSelectionChange(event: Event): void {
    this.stateFacade.setCurrentPageSelection((event.target as HTMLInputElement).checked);
  }

  markSelectedToReview(): void {
    this.stateFacade.updateSelectedReviewStatus('ToReview');
  }

  markSelectedValidated(): void {
    this.stateFacade.updateSelectedReviewStatus('Validated');
  }

  markSelectedLater(): void {
    this.stateFacade.updateSelectedReviewStatus('ToProcessLater');
  }

  markSelectedNotRelevant(): void {
    this.stateFacade.updateSelectedReviewStatus('NotRelevant');
  }

  clearSelection(): void {
    this.stateFacade.clearSelection();
  }

  getStatusSeverity(status: AdminReviewStatus | null | undefined): 'success' | 'info' | 'warn' | 'danger' {
    return getAdminReviewStatusSeverity(status);
  }

  getStatusLabelKey(status: AdminReviewStatus | null | undefined): string {
    return getAdminReviewStatusTranslationKey(status);
  }
}
