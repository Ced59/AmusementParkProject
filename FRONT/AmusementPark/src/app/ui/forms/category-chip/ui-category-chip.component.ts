import { ChangeDetectionStrategy, Component, EventEmitter, HostBinding, Input, Output } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { UiPrimitiveTone } from '@ui/primitives/models/ui-primitive-variant.model';

@Component({
  selector: 'app-ui-category-chip',
  templateUrl: './ui-category-chip.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule]
})
export class UiCategoryChipComponent {
  @Input() labelKey: string | null = null;
  @Input() labelText: string | null = null;
  @Input() value: string | null = null;
  @Input() iconClass: string | null = null;
  @Input() tone: UiPrimitiveTone = 'soft';
  @Input() selected: boolean = false;
  @Input() disabled: boolean = false;

  @Output() selectedValue: EventEmitter<string | null> = new EventEmitter<string | null>();

  @HostBinding('class.app-category-chip') protected readonly appCategoryChipClass: boolean = true;
  @HostBinding('class.app-category-chip--selected') protected get isSelected(): boolean {
    return this.selected;
  }

  @HostBinding('class.app-category-chip--disabled') protected get isDisabled(): boolean {
    return this.disabled;
  }

  protected select(): void {
    if (this.disabled) {
      return;
    }

    this.selectedValue.emit(this.value);
  }
}
