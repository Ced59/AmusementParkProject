import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AttractionWaterExposureLevel } from '@app/models/parks/attraction-water-exposure-level';
import { EntitySelectOption } from '@app/models/shared/entity-select-option';
import { EntitySelectComponent } from '@app/components/shared/entity-select/entity-select.component';
import { Bind } from 'primeng/bind';
import { InputText } from 'primeng/inputtext';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { Select } from 'primeng/select';
import { ButtonDirective } from 'primeng/button';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-admin-park-item-details-tab',
    templateUrl: './admin-park-item-details-tab.component.html',
    styleUrls: ['./admin-park-item-details-tab.component.scss'],
    imports: [FormsModule, ReactiveFormsModule, EntitySelectComponent, Bind, InputText, ToggleSwitch, Select, ButtonDirective, TranslateModule]
})
export class AdminParkItemDetailsTabComponent {
  @Input({ required: true }) formGroup!: FormGroup;
  @Input() manufacturerOptions: EntitySelectOption[] = [];
  @Input() manufacturersLoading: boolean = false;
  @Input() manufacturerAddLink: unknown[] | string | null = null;
  @Input() manufacturerAddQueryParams: Record<string, string | number | boolean | null | undefined> | null = null;
  @Input() waterExposureLevelOptions: Array<{ labelKey: string; value: AttractionWaterExposureLevel }> = [];
  @Input() isSaving: boolean = false;

  @Output() saveSection: EventEmitter<void> = new EventEmitter<void>();
}
