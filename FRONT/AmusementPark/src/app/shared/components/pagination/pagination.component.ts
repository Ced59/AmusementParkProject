import { ChangeDetectionStrategy, Component, ElementRef, EventEmitter, Input, Output } from '@angular/core';
import { NgIf } from '@angular/common';
import { Paginator, PaginatorState } from '@shared/primeless/paginator';
import { PaginationContract } from '@shared/models/contracts';
import { ScrollAnchorService } from '@shared/services/scroll/scroll-anchor.service';

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
  @Input() scrollOnPageChange: boolean = true;
  @Input() scrollTargetSelector: string | null = null;

  @Output() pageChanged: EventEmitter<PaginatorState> = new EventEmitter<PaginatorState>();

  constructor(
    private readonly elementRef: ElementRef<HTMLElement>,
    private readonly scrollAnchorService: ScrollAnchorService
  ) {
  }

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
    const nextFirst: number = event.first ?? this.resolvedFirst;
    const nextRows: number = event.rows ?? this.resolvedRows;
    const isCurrentPageEvent: boolean = nextFirst === this.resolvedFirst && nextRows === this.resolvedRows;

    if (isCurrentPageEvent) {
      return;
    }

    this.pageChanged.emit(event);

    if (!this.scrollOnPageChange) {
      return;
    }

    this.scrollAnchorService.scrollToPaginationTarget(this.elementRef.nativeElement, {
      targetSelector: this.scrollTargetSelector
    });
  }
}
