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
