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

@Component({
  selector: 'app-ui-select',
  standalone: true,
  imports: [FormsModule, NgFor, NgIf],
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => Select), multi: true }],
  template: `
    <input *ngIf="filter" class="p-select-filter" type="search" [attr.aria-label]="filterAriaLabel" [ngModel]="filterText" (ngModelChange)="filterText = $event" [placeholder]="placeholder || ''" [disabled]="isDisabled || loading" (focus)="show()" />
    <select class="p-select-native" [id]="inputId" [attr.aria-label]="ariaLabel" [disabled]="isDisabled || loading" [ngModel]="value" (ngModelChange)="setValue($event)" (focus)="show()" (click)="show()">
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
  @Input() ariaLabel: string | null = null;
  @Input() filterAriaLabel: string | null = null;
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

export { SelectModule } from './select-module';
