import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { NgIf } from '@angular/common';
import { Paginator, PaginatorState } from 'primeng/paginator';
import { PaginationContract } from '@shared/models/contracts';

@Component({
  selector: 'app-pagination',
  templateUrl: './pagination.component.html',
  styleUrls: ['./pagination.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgIf, Paginator]
})
export class PaginationComponent {
  @Input() pagination: PaginationContract | null = null;
  @Input() totalRecords: number | null = null;
  @Input() rows: number | null = null;
  @Input() first: number | null = null;
  @Input() rowsPerPageOptions: number[] = [10, 20, 50];
  @Input() pageLinkSize: number = 3;
  @Input() alwaysShow: boolean = false;

  @Output() pageChanged: EventEmitter<PaginatorState> = new EventEmitter<PaginatorState>();

  protected get resolvedRows(): number {
    return this.rows ?? this.pagination?.itemsPerPage ?? 0;
  }

  protected get resolvedTotalRecords(): number {
    return this.totalRecords ?? this.pagination?.totalItems ?? 0;
  }

  protected get resolvedFirst(): number {
    if (this.first !== null && this.first !== undefined) {
      return this.first;
    }

    if (!this.pagination) {
      return 0;
    }

    return Math.max((this.pagination.currentPage - 1) * this.pagination.itemsPerPage, 0);
  }

  protected get shouldRender(): boolean {
    if (this.resolvedRows <= 0 || this.resolvedTotalRecords <= 0) {
      return false;
    }

    if (this.alwaysShow) {
      return true;
    }

    return this.resolvedTotalRecords > this.resolvedRows;
  }

  protected onPageChange(event: PaginatorState): void {
    this.pageChanged.emit(event);
  }
}
