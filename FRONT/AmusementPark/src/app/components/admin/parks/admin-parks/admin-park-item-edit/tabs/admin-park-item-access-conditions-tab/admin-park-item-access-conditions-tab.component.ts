import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateService, TranslateModule } from '@ngx-translate/core';
import { resolveLocalizedValue } from '@shared/utils/localization';
import {
  ADMIN_PARK_ITEM_HEIGHT_REQUIREMENT_FIELDS,
  AdminParkItemAccessConditionEntry,
  AdminParkItemAccessConditionTypeOption,
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
  @Input() accessConditionPresetOptions: AdminParkItemAccessConditionTypeOption[] = [];
  @Input() accessConditionUnitOptions: Array<{ labelKey: string; value: AttractionAccessConditionUnit }> = [];
  @Input() selectedAccessConditionPreset: string = 'custom';
  @Input() currentLang: string = 'en';
  @Input() isSaving: boolean = false;

  @Output() selectedAccessConditionPresetChange: EventEmitter<string> = new EventEmitter<string>();
  @Output() addAccessCondition: EventEmitter<string> = new EventEmitter<string>();
  @Output() removeAccessCondition: EventEmitter<number> = new EventEmitter<number>();
  @Output() moveAccessConditionUp: EventEmitter<number> = new EventEmitter<number>();
  @Output() moveAccessConditionDown: EventEmitter<number> = new EventEmitter<number>();
  @Output() accessConditionTypeChanged: EventEmitter<number> = new EventEmitter<number>();
  @Output() saveSection: EventEmitter<void> = new EventEmitter<void>();
  @Output() createAccessConditionType: EventEmitter<{ key: string; fr: string; en: string }> = new EventEmitter<{ key: string; fr: string; en: string }>();

  public newAccessConditionTypeKey: string = '';
  public newAccessConditionTypeFrLabel: string = '';
  public newAccessConditionTypeEnLabel: string = '';

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

  get standaloneAccessConditionPresetOptions(): AdminParkItemAccessConditionTypeOption[] {
    return this.accessConditionPresetOptions
      .filter((option: AdminParkItemAccessConditionTypeOption) => !isAdminParkItemHeightAccessConditionType(option.legacyType));
  }

  get safeSelectedAccessConditionPreset(): string {
    const selectedOption: AdminParkItemAccessConditionTypeOption | undefined = this.standaloneAccessConditionPresetOptions
      .find((option: AdminParkItemAccessConditionTypeOption) => option.value === this.selectedAccessConditionPreset);

    return selectedOption?.value ?? this.standaloneAccessConditionPresetOptions[0]?.value ?? 'custom';
  }

  onSelectedAccessConditionPresetChange(value: string): void {
    this.selectedAccessConditionPresetChange.emit(value);
  }

  onAddStandaloneAccessCondition(): void {
    this.addAccessCondition.emit(this.safeSelectedAccessConditionPreset);
  }

  onCreateAccessConditionType(): void {
    const key: string = this.newAccessConditionTypeKey.trim();
    const fr: string = this.newAccessConditionTypeFrLabel.trim();
    const en: string = this.newAccessConditionTypeEnLabel.trim();

    if (!key || !fr) {
      return;
    }

    this.createAccessConditionType.emit({ key, fr, en: en || fr });
    this.newAccessConditionTypeKey = '';
    this.newAccessConditionTypeFrLabel = '';
    this.newAccessConditionTypeEnLabel = '';
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

    const typeKey: string = String(group.get('typeKey')?.value ?? 'custom');
    const option: AdminParkItemAccessConditionTypeOption | undefined = this.accessConditionPresetOptions.find((candidate: AdminParkItemAccessConditionTypeOption) => candidate.value === typeKey);
    if (option) {
      return this.translateService.instant(option.labelKey);
    }

    const type: AttractionAccessConditionType = group.get('type')?.value as AttractionAccessConditionType;
    return this.translateService.instant(getAdminParkItemAccessConditionLabelKey(type));
  }
}
