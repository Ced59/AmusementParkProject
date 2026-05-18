import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateService, TranslateModule } from '@ngx-translate/core';
import { resolveLocalizedValue } from '@shared/utils/localization';
import {
  ADMIN_PARK_ITEM_HEIGHT_REQUIREMENT_FIELDS,
  AdminParkItemAccessConditionEntry,
  AdminParkItemHeightRequirementField,
  AdminParkItemHeightRequirementKey,
  getAdminParkItemAccessConditionLabelKey,
  getAdminParkItemHeightRequirementValue,
  getAdminParkItemStandaloneAccessConditionEntries,
  isAdminParkItemHeightAccessConditionType,
  setAdminParkItemHeightRequirementValue,
  toLocalizedItems
} from '@features/admin/park-items/mappers/admin-park-item-access-condition-form.utils';
import { AttractionAccessConditionType } from '@app/models/parks/attraction-access-condition-type';
import { AttractionAccessConditionUnit } from '@app/models/parks/attraction-access-condition-unit';
import { LocalizedItem } from '@app/models/shared/localized-item';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { Select } from 'primeng/select';
import { ButtonDirective } from 'primeng/button';
import { NgIf, NgFor } from '@angular/common';
import { PrimeTemplate } from 'primeng/api';
import { InputText } from 'primeng/inputtext';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { LocalizedTextInputComponent } from '@app/components/shared/localized-text-input/localized-text-input.component';

@Component({
    selector: 'app-admin-park-item-access-conditions-tab',
    templateUrl: './admin-park-item-access-conditions-tab.component.html',
    styleUrls: ['./admin-park-item-access-conditions-tab.component.scss'],
    imports: [FormsModule, ReactiveFormsModule, Bind, Card, Select, ButtonDirective, NgIf, NgFor, PrimeTemplate, InputText, ToggleSwitch, LocalizedTextInputComponent, TranslateModule]
})
export class AdminParkItemAccessConditionsTabComponent {
  public readonly heightRequirementFields: AdminParkItemHeightRequirementField[] = ADMIN_PARK_ITEM_HEIGHT_REQUIREMENT_FIELDS;

  @Input({ required: true }) formGroup!: FormGroup;
  @Input() accessConditionPresetOptions: Array<{ labelKey: string; value: AttractionAccessConditionType }> = [];
  @Input() accessConditionUnitOptions: Array<{ labelKey: string; value: AttractionAccessConditionUnit }> = [];
  @Input() selectedAccessConditionPreset: AttractionAccessConditionType = 'Custom';
  @Input() currentLang: string = 'en';
  @Input() isSaving: boolean = false;

  @Output() selectedAccessConditionPresetChange: EventEmitter<AttractionAccessConditionType> = new EventEmitter<AttractionAccessConditionType>();
  @Output() addAccessCondition: EventEmitter<AttractionAccessConditionType> = new EventEmitter<AttractionAccessConditionType>();
  @Output() removeAccessCondition: EventEmitter<number> = new EventEmitter<number>();
  @Output() moveAccessConditionUp: EventEmitter<number> = new EventEmitter<number>();
  @Output() moveAccessConditionDown: EventEmitter<number> = new EventEmitter<number>();
  @Output() accessConditionTypeChanged: EventEmitter<number> = new EventEmitter<number>();
  @Output() saveSection: EventEmitter<void> = new EventEmitter<void>();

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly translateService: TranslateService
  ) {
  }

  get accessConditions(): FormArray {
    return this.formGroup.get('accessConditions') as FormArray;
  }

  get standaloneAccessConditionEntries(): AdminParkItemAccessConditionEntry[] {
    return getAdminParkItemStandaloneAccessConditionEntries(this.accessConditions);
  }

  get standaloneAccessConditionPresetOptions(): Array<{ labelKey: string; value: AttractionAccessConditionType }> {
    return this.accessConditionPresetOptions
      .filter((option: { labelKey: string; value: AttractionAccessConditionType }) => !isAdminParkItemHeightAccessConditionType(option.value));
  }

  get safeSelectedAccessConditionPreset(): AttractionAccessConditionType {
    if (!isAdminParkItemHeightAccessConditionType(this.selectedAccessConditionPreset)) {
      return this.selectedAccessConditionPreset;
    }

    return this.standaloneAccessConditionPresetOptions[0]?.value ?? 'Custom';
  }

  onSelectedAccessConditionPresetChange(value: AttractionAccessConditionType): void {
    this.selectedAccessConditionPresetChange.emit(value);
  }

  onAddStandaloneAccessCondition(): void {
    this.addAccessCondition.emit(this.safeSelectedAccessConditionPreset);
  }

  getHeightRequirementValue(key: AdminParkItemHeightRequirementKey): number | null {
    return getAdminParkItemHeightRequirementValue(this.accessConditions, key);
  }

  onHeightRequirementValueChange(key: AdminParkItemHeightRequirementKey, value: unknown): void {
    setAdminParkItemHeightRequirementValue(this.formBuilder, this.accessConditions, key, value);
    this.formGroup.markAsDirty();
  }

  getAccessConditionTitle(index: number): string {
    const group: FormGroup = this.accessConditions.at(index) as FormGroup;
    const label: LocalizedItem<string>[] | null = toLocalizedItems(group.get('label')?.value);
    const resolvedLabel: string | undefined = resolveLocalizedValue(label ?? [], this.currentLang);

    if (resolvedLabel && resolvedLabel.trim().length > 0) {
      return resolvedLabel;
    }

    const type: AttractionAccessConditionType = group.get('type')?.value as AttractionAccessConditionType;
    return this.translateService.instant(getAdminParkItemAccessConditionLabelKey(type));
  }
}
