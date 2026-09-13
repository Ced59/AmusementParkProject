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

export interface PaginatorState {
  page?: number;
  first?: number;
  rows?: number;
  pageCount?: number;
}

@Component({
  selector: 'app-ui-paginator',
  standalone: true,
  imports: [NgFor, FormsModule],
  template: `
    <button type="button" class="p-paginator-element" [disabled]="currentPage <= 0" (click)="goToPage(0)" aria-label="First page"><span class="pi pi-step-backward" aria-hidden="true"></span></button>
    <button type="button" class="p-paginator-element" [disabled]="currentPage <= 0" (click)="goToPage(currentPage - 1)" aria-label="Previous page"><span class="pi pi-chevron-left" aria-hidden="true"></span></button>
    <span class="p-paginator-pages">
      <button *ngFor="let page of visiblePages" type="button" class="p-paginator-page" [class.p-highlight]="page === currentPage" (click)="goToPage(page)">{{ page + 1 }}</button>
    </span>
    <button type="button" class="p-paginator-element" [disabled]="currentPage >= pageCount - 1" (click)="goToPage(currentPage + 1)" aria-label="Next page"><span class="pi pi-chevron-right" aria-hidden="true"></span></button>
    <button type="button" class="p-paginator-element" [disabled]="currentPage >= pageCount - 1" (click)="goToPage(pageCount - 1)" aria-label="Last page"><span class="pi pi-step-forward" aria-hidden="true"></span></button>
    <select class="p-paginator-rpp-options" [ngModel]="rows" (ngModelChange)="changeRows($event)">
      <option *ngFor="let option of rowsPerPageOptions" [ngValue]="option">{{ option }}</option>
    </select>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Paginator {
  @Input() first: number = 0;
  @Input() rows: number = 10;
  @Input() totalRecords: number = 0;
  @Input() rowsPerPageOptions: number[] = [10, 20, 50];
  @Input() pageLinkSize: number = 3;
  @Output() onPageChange: EventEmitter<PaginatorState> = new EventEmitter<PaginatorState>();

  @HostBinding('class.p-paginator') protected readonly paginatorClass: boolean = true;

  get pageCount(): number {
    return Math.max(Math.ceil(this.totalRecords / Math.max(this.rows, 1)), 1);
  }

  get currentPage(): number {
    return Math.min(Math.floor(this.first / Math.max(this.rows, 1)), this.pageCount - 1);
  }

  get visiblePages(): number[] {
    const pageLinkSize: number = Math.max(this.pageLinkSize, 1);
    const start: number = Math.max(0, this.currentPage - Math.floor(pageLinkSize / 2));
    const end: number = Math.min(this.pageCount, start + pageLinkSize);
    const normalizedStart: number = Math.max(0, end - pageLinkSize);
    const pages: number[] = [];
    for (let index: number = normalizedStart; index < end; index += 1) {
      pages.push(index);
    }
    return pages;
  }

  goToPage(page: number): void {
    const normalizedPage: number = Math.max(0, Math.min(page, this.pageCount - 1));
    this.emitChange(normalizedPage, this.rows);
  }

  changeRows(rows: number): void {
    this.emitChange(0, Number(rows));
  }

  private emitChange(page: number, rows: number): void {
    this.onPageChange.emit({
      page,
      rows,
      first: page * rows,
      pageCount: this.pageCount
    });
  }
}

export { PaginatorModule } from './paginator-module';
