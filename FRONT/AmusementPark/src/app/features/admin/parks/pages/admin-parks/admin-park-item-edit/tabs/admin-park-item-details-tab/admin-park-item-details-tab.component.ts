import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AttractionWaterExposureLevel } from '@app/models/parks/attraction-water-exposure-level';
import { AttractionStatus } from '@app/models/parks/attraction-status';
import { EntitySelectOption } from '@app/models/shared/entity-select-option';
import { EntitySelectComponent } from '@shared/components/entity-select/entity-select.component';
import { InputText } from '@shared/ui/primitives/inputtext';
import { ToggleSwitch } from '@shared/ui/primitives/toggleswitch';
import { Select } from '@shared/ui/primitives/select';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-admin-park-item-details-tab',
    templateUrl: './admin-park-item-details-tab.component.html',
    styleUrls: ['./admin-park-item-details-tab.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FormsModule, ReactiveFormsModule, EntitySelectComponent, InputText, ToggleSwitch, Select, ButtonDirective, TranslateModule]
})
export class AdminParkItemDetailsTabComponent {
  @Input({ required: true }) formGroup!: FormGroup;
  @Input() manufacturerOptions: EntitySelectOption[] = [];
  @Input() manufacturersLoading: boolean = false;
  @Input() manufacturerAddLink: unknown[] | string | null = null;
  @Input() manufacturerAddQueryParams: Record<string, string | number | boolean | null | undefined> | null = null;
  @Input() statusOptions: Array<{ labelKey: string; value: AttractionStatus }> = [];
  @Input() waterExposureLevelOptions: Array<{ labelKey: string; value: AttractionWaterExposureLevel }> = [];
  @Input() isSaving: boolean = false;

  @Output() saveSection: EventEmitter<void> = new EventEmitter<void>();
}
