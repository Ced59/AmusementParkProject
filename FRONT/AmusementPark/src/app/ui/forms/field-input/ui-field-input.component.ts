import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

let nextUiFieldInputId: number = 0;

@Component({
  selector: 'app-ui-field-input',
  templateUrl: './ui-field-input.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, TranslateModule]
})
export class UiFieldInputComponent {
  @Input() inputId: string = `ui-field-${nextUiFieldInputId++}`;
  @Input() labelKey: string | null = null;
  @Input() labelText: string | null = null;
  @Input() placeholderKey: string | null = null;
  @Input() placeholderText: string | null = null;
  @Input() value: string = '';
  @Input() type: string = 'text';
  @Input() autocomplete: string | null = null;
  @Input() iconClass: string | null = null;
  @Input() disabled: boolean = false;

  @Output() valueChange: EventEmitter<string> = new EventEmitter<string>();

  protected get hasLabel(): boolean {
    return !!this.labelKey || !!this.labelText;
  }

  protected get resolvedPlaceholder(): string {
    return this.placeholderText ?? '';
  }

  protected onValueChanged(value: string): void {
    this.valueChange.emit(value ?? '');
  }
}
