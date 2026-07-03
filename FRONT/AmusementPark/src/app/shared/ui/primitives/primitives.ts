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
  Output,
  QueryList,
  Renderer2,
  signal,
  TemplateRef,
  WritableSignal,
} from '@angular/core';
import { FormsModule, NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';
import { NgClass, NgFor, NgIf, NgStyle, NgTemplateOutlet } from '@angular/common';
import { TranslateService } from '@ngx-translate/core';
import { UiTemplate } from './api';

export { MessageService, UiTemplate, ToastMessage } from './api';

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

@Directive({
  selector: '[appUiBind]',
  standalone: true
})
export class Bind {
}

@Directive({
  selector: '[appUiRipple]',
  standalone: true
})
export class Ripple {
  @HostBinding('class.p-ripple') protected readonly rippleClass: boolean = true;
}

@Directive({
  selector: 'input[appUiInputText], textarea[appUiInputText]',
  standalone: true
})
export class InputText {
  @HostBinding('class.p-inputtext') protected readonly inputClass: boolean = true;
  @HostBinding('class.app-input') protected readonly appInputClass: boolean = true;
}

@Directive({
  selector: '[appUiTooltip]',
  standalone: true
})
export class Tooltip implements OnChanges {
  @Input('appUiTooltip') text: string | null = null;

  constructor(private readonly elementRef: ElementRef<HTMLElement>) {
  }

  ngOnChanges(): void {
    if (this.text) {
      this.elementRef.nativeElement.setAttribute('title', this.text);
    } else {
      this.elementRef.nativeElement.removeAttribute('title');
    }
  }
}

@Directive({
  selector: 'button[appUiButton], a[appUiButton]',
  standalone: true
})
export class ButtonDirective implements OnChanges, AfterViewInit {
  @Input() label: string | null = null;
  @Input() icon: string | null = null;
  @Input() iconPos: 'left' | 'right' | 'top' | 'bottom' = 'left';
  @Input() severity: string | null = null;
  @Input() styleClass: string | null = null;
  @Input() text: boolean | string = false;
  @Input() outlined: boolean | string = false;
  @Input() rounded: boolean | string = false;
  @Input() link: boolean | string = false;
  @Input() loading: boolean = false;

  @HostBinding('class.p-button') protected readonly buttonClass: boolean = true;
  @HostBinding('class.app-compatible-button') protected readonly appButtonClass: boolean = true;
  @HostBinding('class.p-button-loading') protected get isLoading(): boolean {
    return this.loading;
  }
  @HostBinding('class.p-button-text') protected get isText(): boolean {
    return this.asBoolean(this.text);
  }
  @HostBinding('class.p-button-outlined') protected get isOutlined(): boolean {
    return this.asBoolean(this.outlined);
  }
  @HostBinding('class.p-button-rounded') protected get isRounded(): boolean {
    return this.asBoolean(this.rounded);
  }
  @HostBinding('class.p-button-link') protected get isLink(): boolean {
    return this.asBoolean(this.link);
  }
  @HostBinding('class.p-button-secondary') protected get isSecondary(): boolean {
    return this.severity === 'secondary';
  }
  @HostBinding('class.p-button-success') protected get isSuccess(): boolean {
    return this.severity === 'success';
  }
  @HostBinding('class.p-button-info') protected get isInfo(): boolean {
    return this.severity === 'info';
  }
  @HostBinding('class.p-button-warning') protected get isWarning(): boolean {
    return this.severity === 'warn' || this.severity === 'warning';
  }
  @HostBinding('class.p-button-danger') protected get isDanger(): boolean {
    return this.severity === 'danger';
  }
  @HostBinding('class.p-button-help') protected get isHelp(): boolean {
    return this.severity === 'help';
  }

  private hasView: boolean = false;

  constructor(
    private readonly elementRef: ElementRef<HTMLElement>,
    private readonly renderer: Renderer2
  ) {
  }

  ngAfterViewInit(): void {
    this.hasView = true;
    this.renderContent();
  }

  ngOnChanges(): void {
    if (this.hasView) {
      this.renderContent();
    }
  }

  private renderContent(): void {
    const host: HTMLElement = this.elementRef.nativeElement;
    if (!this.label && !this.icon && !this.loading) {
      return;
    }

    while (host.firstChild) {
      this.renderer.removeChild(host, host.firstChild);
    }

    const resolvedIcon: string | null = this.loading ? 'pi pi-spin pi-spinner' : this.icon;
    if (resolvedIcon && (this.iconPos === 'left' || this.iconPos === 'top')) {
      this.appendIcon(host, resolvedIcon);
    }

    if (this.label) {
      const labelElement: HTMLElement = this.renderer.createElement('span');
      this.renderer.addClass(labelElement, 'p-button-label');
      this.renderer.appendChild(labelElement, this.renderer.createText(this.label));
      this.renderer.appendChild(host, labelElement);
    }

    if (resolvedIcon && (this.iconPos === 'right' || this.iconPos === 'bottom')) {
      this.appendIcon(host, resolvedIcon);
    }
  }

  private appendIcon(host: HTMLElement, icon: string): void {
    const iconElement: HTMLElement = this.renderer.createElement('span');
    this.renderer.addClass(iconElement, 'p-button-icon');
    this.renderer.addClass(iconElement, 'app-button-icon');
    for (const className of icon.split(' ').filter((value: string) => value.length > 0)) {
      this.renderer.addClass(iconElement, className);
    }
    this.renderer.setAttribute(iconElement, 'aria-hidden', 'true');
    this.renderer.appendChild(host, iconElement);
  }

  private asBoolean(value: boolean | string): boolean {
    return value === true || value === '';
  }
}

@Component({
  selector: 'app-ui-card',
  standalone: true,
  imports: [NgIf, NgTemplateOutlet],
  template: `
    <ng-container *ngIf="template('header') as headerTemplate">
      <div class="p-card-header">
        <ng-container *ngTemplateOutlet="headerTemplate"></ng-container>
      </div>
    </ng-container>
    <div *ngIf="!template('header') && (header || subheader)" class="p-card-header p-card-header--text">
      <div *ngIf="header" class="p-card-title">{{ header }}</div>
      <div *ngIf="subheader" class="p-card-subtitle">{{ subheader }}</div>
    </div>
    <div class="p-card-body">
      <div class="p-card-content">
        <ng-content></ng-content>
      </div>
    </div>
    <ng-container *ngIf="template('footer') as footerTemplate">
      <div class="p-card-footer">
        <ng-container *ngTemplateOutlet="footerTemplate"></ng-container>
      </div>
    </ng-container>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Card implements AfterContentInit {
  @Input() header: string | null = null;
  @Input() subheader: string | null = null;
  @Input() styleClass: string | null = null;
  @ContentChildren(UiTemplate) templates!: QueryList<UiTemplate>;

  @HostBinding('class') protected get hostClasses(): string {
    return `p-card ${this.styleClass ?? ''}`.trim();
  }

  ngAfterContentInit(): void {
  }

  template(name: string): TemplateRef<unknown> | null {
    return this.templates?.find((template: UiTemplate) => template.name === name)?.template ?? null;
  }
}

@Component({
  selector: 'app-ui-tag',
  standalone: true,
  template: `<span class="p-tag-value">{{ value }}</span><ng-content></ng-content>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Tag {
  @Input() value: string | number | null = null;
  @Input() severity: string | null = null;

  @HostBinding('class') protected get hostClasses(): string {
    return ['p-tag', this.severity ? `p-tag-${this.severity}` : null].filter((value: string | null) => value !== null).join(' ');
  }
}

@Component({
  selector: 'app-ui-progress-spinner',
  standalone: true,
  template: `<span class="pi pi-spin pi-spinner" aria-hidden="true"></span>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProgressSpinner {
  @Input() styleClass: string | null = null;

  @HostBinding('class') protected get hostClasses(): string {
    return `p-progress-spinner ${this.styleClass ?? ''}`.trim();
  }
}

@Component({
  selector: 'app-ui-progress-bar',
  standalone: true,
  template: `<div class="p-progressbar-value" [style.width.%]="normalizedValue"></div>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProgressBar {
  @Input() value: number | null = 0;

  @HostBinding('class.p-progressbar') protected readonly progressClass: boolean = true;

  protected get normalizedValue(): number {
    const value: number = Number(this.value ?? 0);
    return Math.max(0, Math.min(100, Number.isFinite(value) ? value : 0));
  }
}

@Component({
  selector: 'app-ui-panel',
  standalone: true,
  imports: [NgIf],
  template: `
    <div class="p-panel-header" (click)="toggle()" [class.p-panel-header--toggleable]="toggleable">
      <span>{{ header }}</span>
      <button *ngIf="toggleable" type="button" class="p-panel-toggle" [attr.aria-expanded]="!collapsed">
        <span [class]="collapsed ? 'pi pi-chevron-down' : 'pi pi-chevron-up'" aria-hidden="true"></span>
      </button>
    </div>
    <div class="p-panel-content" *ngIf="!collapsed">
      <ng-content></ng-content>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Panel {
  @Input() header: string | null = null;
  @Input() toggleable: boolean = false;
  @Input() collapsed: boolean = false;

  @HostBinding('class.p-panel') protected readonly panelClass: boolean = true;

  toggle(): void {
    if (this.toggleable) {
      this.collapsed = !this.collapsed;
    }
  }
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

@Component({
  selector: 'app-ui-sort-icon',
  standalone: true,
  template: `<span class="pi pi-sort-alt" aria-hidden="true"></span>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SortIcon {
  @Input() field: string | null = null;
}

@Directive({
  selector: '[appUiSortableColumn]',
  standalone: true
})
export class SortableColumn {
  @Input('appUiSortableColumn') field: string = '';
  @HostBinding('class.p-sortable-column') protected readonly sortableClass: boolean = true;
}

@Component({
  selector: 'app-ui-select',
  standalone: true,
  imports: [FormsModule, NgFor, NgIf],
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => Select), multi: true }],
  template: `
    <input *ngIf="filter" class="p-select-filter" type="search" [ngModel]="filterText" (ngModelChange)="filterText = $event" [placeholder]="placeholder || ''" [disabled]="isDisabled || loading" (focus)="show()" />
    <select class="p-select-native" [id]="inputId" [disabled]="isDisabled || loading" [ngModel]="value" (ngModelChange)="setValue($event)" (focus)="show()" (click)="show()">
      <option *ngIf="showClear" [ngValue]="null">{{ placeholder || '-' }}</option>
      <option *ngFor="let option of filteredOptions" [ngValue]="resolveOptionValue(option)">{{ resolveOptionLabel(option) }}</option>
    </select>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Select implements ControlValueAccessor {
  @Input() options: readonly unknown[] = [];
  @Input() optionLabel: string = 'label';
  @Input() optionValue: string = 'value';
  @Input() inputId: string | null = null;
  @Input() placeholder: string | null = null;
  @Input() showClear: boolean = false;
  @Input() filter: boolean = false;
  @Input() appendTo: string | null = null;
  @Input() styleClass: string | null = null;
  @Input() disabled: boolean = false;
  @Input() loading: boolean = false;
  @Output() onChange: EventEmitter<{ value: unknown }> = new EventEmitter<{ value: unknown }>();
  @Output() onShow: EventEmitter<void> = new EventEmitter<void>();

  value: unknown = null;
  filterText: string = '';
  private onValueChange: (value: unknown) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(private readonly translateService: TranslateService) {
  }

  @HostBinding('class') protected get hostClasses(): string {
    return `p-select ${this.styleClass ?? ''}`.trim();
  }

  get isDisabled(): boolean {
    return this.disabled;
  }

  get filteredOptions(): readonly unknown[] {
    const normalizedFilter: string = this.filterText.trim().toLowerCase();
    if (!normalizedFilter) {
      return this.options;
    }

    return this.options.filter((option: unknown): boolean => this.resolveOptionLabel(option).toLowerCase().includes(normalizedFilter));
  }

  writeValue(value: unknown): void {
    this.value = value ?? null;
  }

  registerOnChange(fn: (value: unknown) => void): void {
    this.onValueChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  setValue(value: unknown): void {
    this.value = value ?? null;
    this.onValueChange(this.value);
    this.onChange.emit({ value: this.value });
    this.onTouched();
  }

  show(): void {
    this.onShow.emit();
  }

  resolveOptionValue(option: unknown): unknown {
    if (option && typeof option === 'object' && this.optionValue in option) {
      return (option as Record<string, unknown>)[this.optionValue];
    }
    return option;
  }

  resolveOptionLabel(option: unknown): string {
    const rawValue: unknown = this.resolveOptionLabelValue(option);

    if (typeof rawValue !== 'string') {
      return rawValue === null || rawValue === undefined ? '' : String(rawValue);
    }

    const translatedValue: string = this.translateService.instant(rawValue);
    return translatedValue || rawValue;
  }

  private resolveOptionLabelValue(option: unknown): unknown {
    if (!option || typeof option !== 'object') {
      return option;
    }

    const record: Record<string, unknown> = option as Record<string, unknown>;
    if (this.optionLabel in record) {
      return record[this.optionLabel];
    }

    if ('labelKey' in record) {
      return record['labelKey'];
    }

    if ('name' in record) {
      return record['name'];
    }

    if (this.optionValue in record) {
      return record[this.optionValue];
    }

    return '';
  }
}

@Component({
  selector: 'app-ui-checkbox',
  standalone: true,
  imports: [FormsModule],
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => Checkbox), multi: true }],
  template: `<input class="p-checkbox-input" type="checkbox" [id]="inputId" [disabled]="disabled" [ngModel]="checked" (ngModelChange)="setChecked($event)" />`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Checkbox implements ControlValueAccessor {
  @Input() binary: boolean = true;
  @Input() inputId: string | null = null;
  @Input() disabled: boolean = false;
  checked: boolean = false;
  private onChange: (value: boolean) => void = () => {};
  private onTouched: () => void = () => {};

  @HostBinding('class.p-checkbox') protected readonly checkboxClass: boolean = true;

  writeValue(value: unknown): void {
    this.checked = value === true;
  }

  registerOnChange(fn: (value: boolean) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  setChecked(value: boolean): void {
    this.checked = value;
    this.onChange(value);
    this.onTouched();
  }
}

@Component({
  selector: 'app-ui-toggle-switch',
  standalone: true,
  imports: [FormsModule],
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => ToggleSwitch), multi: true }],
  template: `
    <label class="p-toggleswitch-control">
      <input type="checkbox" [disabled]="disabled" [ngModel]="checked" (ngModelChange)="setChecked($event)" />
      <span class="p-toggleswitch-slider"></span>
    </label>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ToggleSwitch implements ControlValueAccessor {
  @Input() disabled: boolean = false;
  checked: boolean = false;
  private onChange: (value: boolean) => void = () => {};
  private onTouched: () => void = () => {};

  @HostBinding('class.p-toggleswitch') protected readonly switchClass: boolean = true;
  @HostBinding('class.p-toggleswitch-checked') protected get checkedClass(): boolean {
    return this.checked;
  }

  writeValue(value: unknown): void {
    this.checked = value === true;
  }

  registerOnChange(fn: (value: boolean) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  setChecked(value: boolean): void {
    this.checked = value;
    this.onChange(value);
    this.onTouched();
  }
}

@Component({
  selector: 'app-ui-input-number',
  standalone: true,
  imports: [FormsModule],
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => InputNumber), multi: true }],
  template: `
    <input class="p-inputnumber-input p-inputtext app-input" type="number" [id]="inputId" [min]="min" [placeholder]="placeholder || ''" [disabled]="disabled" [ngModel]="value" (ngModelChange)="setValue($event)" />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class InputNumber implements ControlValueAccessor {
  @Input() inputId: string | null = null;
  @Input() min: number | null = null;
  @Input() showButtons: boolean = false;
  @Input() placeholder: string | null = null;
  @Input() disabled: boolean = false;
  value: number | null = null;
  private onChange: (value: number | null) => void = () => {};
  private onTouched: () => void = () => {};

  @HostBinding('class.p-inputnumber') protected readonly inputNumberClass: boolean = true;

  writeValue(value: unknown): void {
    this.value = typeof value === 'number' ? value : value === null || value === undefined || value === '' ? null : Number(value);
  }

  registerOnChange(fn: (value: number | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  setValue(value: unknown): void {
    const numericValue: number | null = value === null || value === undefined || value === '' ? null : Number(value);
    this.value = numericValue;
    this.onChange(numericValue);
    this.onTouched();
  }
}

@Component({
  selector: 'app-ui-tabs',
  standalone: true,
  template: `<ng-content></ng-content>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Tabs {
  private readonly valueSignal: WritableSignal<string | number> = signal(0);

  @Input()
  set value(value: string | number) {
    this.valueSignal.set(value);
  }

  get value(): string | number {
    return this.valueSignal();
  }

  @Output() valueChange: EventEmitter<string | number> = new EventEmitter<string | number>();

  @HostBinding('class.p-tabs') protected readonly tabsClass: boolean = true;

  setValue(value: string | number): void {
    const normalizedValue: string | number = this.normalizeSelectedValue(value);
    this.valueSignal.set(normalizedValue);
    this.valueChange.emit(normalizedValue);
  }

  isValueActive(value: string | number): boolean {
    return this.normalizeValue(this.value) === this.normalizeValue(value);
  }

  private normalizeSelectedValue(value: string | number): string | number {
    if (typeof this.value === 'number') {
      const numericValue: number = typeof value === 'number' ? value : Number(value);
      return Number.isFinite(numericValue) ? numericValue : value;
    }

    return String(value);
  }

  private normalizeValue(value: string | number): string {
    return String(value);
  }
}

@Component({
  selector: 'app-ui-tab-list',
  standalone: true,
  template: `<div class="p-tablist-tab-list"><ng-content></ng-content></div>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TabList {
  @HostBinding('class.p-tablist') protected readonly tabListClass: boolean = true;
}

@Component({
  selector: 'app-ui-tab',
  standalone: true,
  template: `<button type="button" class="p-tab-button" [disabled]="disabled" (click)="select($event)"><ng-content></ng-content></button>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Tab {
  @Input() value: string | number = 0;
  @Input() disabled: boolean = false;

  @HostBinding('class.p-tab') protected readonly tabClass: boolean = true;
  @HostBinding('class.p-tab-active') protected get activeClass(): boolean {
    return this.tabs.isValueActive(this.value);
  }

  constructor(private readonly tabs: Tabs) {
  }

  @HostListener('click', ['$event'])
  selectFromHost(event: MouseEvent): void {
    const target: EventTarget | null = event.target;
    if (target instanceof HTMLElement && target.closest('.p-tab-button')) {
      return;
    }

    this.select();
  }

  select(event?: MouseEvent): void {
    event?.stopPropagation();

    if (!this.disabled) {
      this.tabs.setValue(this.value);
    }
  }
}

@Component({
  selector: 'app-ui-tab-panels',
  standalone: true,
  template: `<ng-content></ng-content>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TabPanels {
  @HostBinding('class.p-tabpanels') protected readonly tabPanelsClass: boolean = true;
}

@Component({
  selector: 'app-ui-tab-panel',
  standalone: true,
  imports: [NgIf],
  template: `<ng-container *ngIf="isActive"><ng-content></ng-content></ng-container>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TabPanel {
  @Input() value: string | number = 0;
  @HostBinding('class.p-tabpanel') protected readonly tabPanelClass: boolean = true;

  constructor(protected readonly tabs: Tabs) {
  }

  get isActive(): boolean {
    return this.tabs.isValueActive(this.value);
  }
}

@Component({
  selector: 'app-ui-dialog',
  standalone: true,
  imports: [NgIf, NgStyle, NgClass, NgTemplateOutlet],
  template: `
    <div *ngIf="visible" class="p-dialog-mask" [class.p-dialog-mask--modal]="modal" (click)="onMaskClick($event)">
      <section class="p-dialog" [ngClass]="styleClass" [ngStyle]="style" role="dialog" aria-modal="true">
        <header *ngIf="showHeader" class="p-dialog-header">
          <span class="p-dialog-title">{{ header }}</span>
          <button *ngIf="closable" type="button" class="p-dialog-header-close" (click)="hide()"><span class="pi pi-times" aria-hidden="true"></span></button>
        </header>
        <div class="p-dialog-content"><ng-content></ng-content></div>
        <ng-container *ngIf="template('footer') as footerTemplate">
          <footer class="p-dialog-footer"><ng-container *ngTemplateOutlet="footerTemplate"></ng-container></footer>
        </ng-container>
      </section>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Dialog implements AfterContentInit {
  @Input() visible: boolean = false;
  @Input() header: string | null = null;
  @Input() modal: boolean = false;
  @Input() dismissableMask: boolean = false;
  @Input() closable: boolean = true;
  @Input() draggable: boolean = false;
  @Input() resizable: boolean = false;
  @Input() showHeader: boolean = true;
  @Input() style: Record<string, string> | null = null;
  @Input() styleClass: string | null = null;
  @Output() visibleChange: EventEmitter<boolean> = new EventEmitter<boolean>();
  @Output() onHide: EventEmitter<void> = new EventEmitter<void>();
  @ContentChildren(UiTemplate) templates!: QueryList<UiTemplate>;

  ngAfterContentInit(): void {
  }

  hide(): void {
    this.visible = false;
    this.visibleChange.emit(false);
    this.onHide.emit();
  }

  onMaskClick(event: Event): void {
    if (this.dismissableMask && event.target instanceof HTMLElement && event.target.classList.contains('p-dialog-mask')) {
      this.hide();
    }
  }

  template(name: string): TemplateRef<unknown> | null {
    return this.templates?.find((template: UiTemplate) => template.name === name)?.template ?? null;
  }
}

@NgModule({ imports: [ButtonDirective], exports: [ButtonDirective] })
export class ButtonModule {}

@NgModule({ imports: [Card], exports: [Card] })
export class CardModule {}

@NgModule({ imports: [InputText], exports: [InputText] })
export class InputTextModule {}

@NgModule({ imports: [Select], exports: [Select] })
export class SelectModule {}

@NgModule({ imports: [Paginator], exports: [Paginator] })
export class PaginatorModule {}

@NgModule({ imports: [Table, SortIcon, SortableColumn, UiTemplate], exports: [Table, SortIcon, SortableColumn, UiTemplate] })
export class TableModule {}

@NgModule({ imports: [Tag], exports: [Tag] })
export class TagModule {}

@NgModule({ imports: [ToggleSwitch], exports: [ToggleSwitch] })
export class ToggleSwitchModule {}

@NgModule({ imports: [Tooltip], exports: [Tooltip] })
export class TooltipModule {}

@NgModule({ imports: [ProgressSpinner], exports: [ProgressSpinner] })
export class ProgressSpinnerModule {}

@NgModule({ imports: [Panel], exports: [Panel] })
export class PanelModule {}

@NgModule({ imports: [Tabs, TabList, Tab, TabPanels, TabPanel], exports: [Tabs, TabList, Tab, TabPanels, TabPanel] })
export class TabsModule {}

@NgModule({ imports: [], exports: [] })
export class DividerModule {}
