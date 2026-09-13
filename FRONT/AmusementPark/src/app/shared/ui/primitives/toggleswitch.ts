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

export { ToggleSwitchModule } from './toggle-switch-module';
