import { Component, forwardRef, Input } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';
import { EntitySelectOption } from '../../../models/shared/entity-select-option';
import { Bind } from 'primeng/bind';
import { Select } from 'primeng/select';
import { ButtonDirective } from 'primeng/button';
import { RouterLink } from '@angular/router';

@Component({
    selector: 'app-entity-select',
    templateUrl: './entity-select.component.html',
    styleUrls: ['./entity-select.component.scss'],
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => EntitySelectComponent),
            multi: true
        }
    ],
    imports: [Bind, Select, FormsModule, ButtonDirective, RouterLink]
})
export class EntitySelectComponent implements ControlValueAccessor {
  @Input() label: string = '';
  @Input() placeholder: string = '';
  @Input() options: EntitySelectOption[] = [];
  @Input() addLink: any[] | string | null = null;
  @Input() addQueryParams: Record<string, string | number | boolean | null | undefined> | null = null;
  @Input() addButtonLabel: string = '';
  @Input() loading: boolean = false;

  value: string | null = null;
  isDisabled: boolean = false;

  private onChange: (value: string | null) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: string | null | undefined): void {
    this.value = value ?? null;
  }

  registerOnChange(fn: (value: string | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled = isDisabled;
  }

  onValueChange(value: string | null): void {
    this.value = value;
    this.onChange(value);
    this.onTouched();
  }
}
