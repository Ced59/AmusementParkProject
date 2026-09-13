import {
  AfterContentInit,
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ContentChildren,
  Directive,
  ElementRef,
  EventEmitter,
  forwardRef,
  HostBinding,
  HostListener,
  Input,
  NgModule,
  OnChanges,
  OnDestroy,
  Output,
  QueryList,
  Renderer2,
  TemplateRef,
} from '@angular/core';

import { FormsModule, NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';

import { NgClass, NgFor, NgIf, NgStyle, NgTemplateOutlet } from '@angular/common';

import { TranslateService } from '@ngx-translate/core';

import { UiTemplate } from './ui-template';

import { Paginator } from './paginator';

export interface PaginatorState {
  page?: number;
  first?: number;
  rows?: number;
  pageCount?: number;
}

export interface TableLazyLoadEvent {
  first?: number;
  rows?: number;
  sortField?: string | string[] | null;
  sortOrder?: number | null;
}

export interface TableSortEvent {
  field?: string | string[] | null;
  order?: number | null;
}

@Component({
  selector: 'app-ui-table',
  standalone: true,
  imports: [NgFor, NgIf, NgTemplateOutlet, Paginator],
  template: `
    <div class="p-datatable-wrapper" (click)="onTableClick($event)">
      <table class="p-datatable-table">
        <thead class="p-datatable-thead">
          <ng-container *ngIf="template('header') as headerTemplate">
            <ng-container *ngTemplateOutlet="headerTemplate"></ng-container>
          </ng-container>
        </thead>
        <tbody class="p-datatable-tbody">
          <ng-container *ngIf="!loading && value.length > 0 && template('body') as bodyTemplate">
            <ng-container *ngFor="let row of value">
              <ng-container *ngTemplateOutlet="bodyTemplate; context: { $implicit: row }"></ng-container>
            </ng-container>
          </ng-container>
          <ng-container *ngIf="!loading && value.length === 0 && template('emptymessage') as emptyTemplate">
            <ng-container *ngTemplateOutlet="emptyTemplate"></ng-container>
          </ng-container>
          <tr *ngIf="loading">
            <td class="p-datatable-loading-cell" [attr.colspan]="100">
              <span class="pi pi-spin pi-spinner" aria-hidden="true"></span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    <app-ui-paginator *ngIf="paginator" [first]="first" [rows]="rows" [totalRecords]="totalRecords" [rowsPerPageOptions]="rowsPerPageOptions" (onPageChange)="onPaginatorChange($event)"></app-ui-paginator>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Table implements AfterContentInit {
  @Input() value: readonly unknown[] = [];
  @Input() loading: boolean = false;
  @Input() paginator: boolean = false;
  @Input() rows: number = 10;
  @Input() totalRecords: number = 0;
  @Input() lazy: boolean = false;
  @Input() sortField: string | string[] | null | undefined = null;
  @Input() sortOrder: number | null | undefined = null;
  @Input() first: number = 0;
  @Input() responsiveLayout: string | null = null;
  @Input() styleClass: string | null = null;
  @Input() rowsPerPageOptions: number[] = [10, 20, 50];
  @Output() onLazyLoad: EventEmitter<TableLazyLoadEvent> = new EventEmitter<TableLazyLoadEvent>();
  @Output() onSort: EventEmitter<TableSortEvent> = new EventEmitter<TableSortEvent>();
  @ContentChildren(UiTemplate) templates!: QueryList<UiTemplate>;
  @HostBinding('class') protected get hostClasses(): string {
    return `p-datatable ${this.styleClass ?? ''}`.trim();
  }

  ngAfterContentInit(): void {
  }

  template(name: string): TemplateRef<unknown> | null {
    return this.templates?.find((template: UiTemplate) => template.name === name)?.template ?? null;
  }

  onPaginatorChange(event: PaginatorState): void {
    this.first = event.first ?? 0;
    this.onLazyLoad.emit({
      first: event.first,
      rows: event.rows,
      sortField: this.sortField,
      sortOrder: this.sortOrder
    });
  }

  onTableClick(event: Event): void {
    const target: HTMLElement | null = event.target instanceof HTMLElement ? event.target : null;
    const sortableHeader: HTMLElement | null = target?.closest('[appUiSortableColumn]') ?? null;
    if (!sortableHeader) {
      return;
    }

    const field: string | null = sortableHeader.getAttribute('appUiSortableColumn');
    if (!field) {
      return;
    }

    const nextOrder: number = this.sortField === field && this.sortOrder === 1 ? -1 : 1;
    this.sortField = field;
    this.sortOrder = nextOrder;
    this.first = 0;
    this.onSort.emit({
      field,
      order: nextOrder
    });
    this.onLazyLoad.emit({
      first: 0,
      rows: this.rows,
      sortField: field,
      sortOrder: nextOrder
    });
  }
}

export { SortableColumn } from './sortable-column';
export { SortIcon } from './sort-icon';
export { TableModule } from './table-module';
