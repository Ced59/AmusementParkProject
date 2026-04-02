import { Component, EventEmitter, Input, Output } from '@angular/core';
import { AbstractControl, FormGroup } from '@angular/forms';
import { EntitySelectOption } from '../../../../../../../models/shared/entity-select-option';
import { ParkType } from '../../../../../../../models/parks/park-type';

interface ParkTypeOption {
  labelKey: string;
  value: ParkType;
}

@Component({
    selector: 'app-admin-park-general-tab',
    templateUrl: './admin-park-general-tab.component.html',
    styleUrls: ['./admin-park-general-tab.component.scss'],
    standalone: false
})
export class AdminParkGeneralTabComponent {
  @Input({ required: true }) form!: FormGroup;
  @Input() parkTypeOptions: ParkTypeOption[] = [];
  @Input() countryOptions: { code: string; label: string }[] = [];
  @Input() founderOptions: EntitySelectOption[] = [];
  @Input() operatorOptions: EntitySelectOption[] = [];
  @Input() foundersLoading: boolean = false;
  @Input() operatorsLoading: boolean = false;
  @Input() founderAddLink: string[] = [];
  @Input() founderAddQueryParams: Record<string, string | number> = {};
  @Input() operatorAddLink: string[] = [];
  @Input() operatorAddQueryParams: Record<string, string | number> = {};
  @Input() currentLang: string = 'en';
  @Input() parkId: string | null = null;
  @Input() isEditMode: boolean = false;
  @Input() isSaving: boolean = false;

  @Output() saveSection: EventEmitter<void> = new EventEmitter<void>();

  get nameControl(): AbstractControl | null {
    return this.form.get('name');
  }

  get isVisibleControl(): AbstractControl | null {
    return this.form.get('isVisible');
  }
}
