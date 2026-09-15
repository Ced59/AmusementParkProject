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
import { PaginationLabels, resolvePaginationLabels } from '@shared/utils/pagination/pagination-labels';

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
    @if (pageHref && currentPage > 0) {
      <a class="p-paginator-element" [href]="pageHref(0)" (click)="onLinkClick($event, 0)" [attr.aria-label]="linkLabels.first"><span class="pi pi-step-backward" aria-hidden="true"></span><span class="app-visually-hidden">{{ linkLabels.first }}</span></a>
      <a class="p-paginator-element" rel="prev" [href]="pageHref(currentPage - 1)" (click)="onLinkClick($event, currentPage - 1)" [attr.aria-label]="linkLabels.previous"><span class="pi pi-chevron-left" aria-hidden="true"></span><span class="app-visually-hidden">{{ linkLabels.previous }}</span></a>
    } @else {
    <button type="button" class="p-paginator-element" [disabled]="currentPage <= 0" (click)="goToPage(0)" aria-label="First page"><span class="pi pi-step-backward" aria-hidden="true"></span></button>
    <button type="button" class="p-paginator-element" [disabled]="currentPage <= 0" (click)="goToPage(currentPage - 1)" aria-label="Previous page"><span class="pi pi-chevron-left" aria-hidden="true"></span></button>
    }
    <span class="p-paginator-pages">
      @for (page of visiblePages; track page) {
        @if (pageHref) {
          <a class="p-paginator-page" [class.p-highlight]="page === currentPage" [attr.aria-current]="page === currentPage ? 'page' : null" [attr.aria-label]="linkLabels.page + ' ' + (page + 1)" [href]="pageHref(page)" (click)="onLinkClick($event, page)">{{ page + 1 }}</a>
        } @else {
          <button type="button" class="p-paginator-page" [class.p-highlight]="page === currentPage" (click)="goToPage(page)">{{ page + 1 }}</button>
        }
      }
    </span>
    @if (pageHref && currentPage < pageCount - 1) {
      <a class="p-paginator-element" rel="next" [href]="pageHref(currentPage + 1)" (click)="onLinkClick($event, currentPage + 1)" [attr.aria-label]="linkLabels.next"><span class="pi pi-chevron-right" aria-hidden="true"></span><span class="app-visually-hidden">{{ linkLabels.next }}</span></a>
      <a class="p-paginator-element" [href]="pageHref(pageCount - 1)" (click)="onLinkClick($event, pageCount - 1)" [attr.aria-label]="linkLabels.last"><span class="pi pi-step-forward" aria-hidden="true"></span><span class="app-visually-hidden">{{ linkLabels.last }}</span></a>
    } @else {
    <button type="button" class="p-paginator-element" [disabled]="currentPage >= pageCount - 1" (click)="goToPage(currentPage + 1)" aria-label="Next page"><span class="pi pi-chevron-right" aria-hidden="true"></span></button>
    <button type="button" class="p-paginator-element" [disabled]="currentPage >= pageCount - 1" (click)="goToPage(pageCount - 1)" aria-label="Last page"><span class="pi pi-step-forward" aria-hidden="true"></span></button>
    }
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
  @Input() pageHref: ((page: number) => string) | null = null;
  @Input() pageLanguage: string = 'en';
  @Output() onPageChange: EventEmitter<PaginatorState> = new EventEmitter<PaginatorState>();

  @HostBinding('class.p-paginator') protected readonly paginatorClass: boolean = true;

  protected get linkLabels(): PaginationLabels {
    return resolvePaginationLabels(this.pageLanguage);
  }

  protected onLinkClick(event: MouseEvent, page: number): void {
    if (event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) {
      return;
    }
    event.preventDefault();
    this.goToPage(page);
  }

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
