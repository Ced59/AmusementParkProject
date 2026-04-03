import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormArray, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateService, TranslateModule } from '@ngx-translate/core';
import { resolveLocalizedValue } from '../../../../../../../commons/localized-item.utils';
import { AttractionAccessConditionType } from '../../../../../../../models/parks/attraction-access-condition-type';
import { AttractionAccessConditionUnit } from '../../../../../../../models/parks/attraction-access-condition-unit';
import { LocalizedItem } from '../../../../../../../models/shared/localized-item';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { Select } from 'primeng/select';
import { ButtonDirective } from 'primeng/button';
import { NgIf, NgFor } from '@angular/common';
import { PrimeTemplate } from 'primeng/api';
import { InputText } from 'primeng/inputtext';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { LocalizedTextInputComponent } from '../../../../../../shared/localized-text-input/localized-text-input.component';

@Component({
    selector: 'app-admin-park-item-access-conditions-tab',
    templateUrl: './admin-park-item-access-conditions-tab.component.html',
    styleUrls: ['./admin-park-item-access-conditions-tab.component.scss'],
    imports: [FormsModule, ReactiveFormsModule, Bind, Card, Select, ButtonDirective, NgIf, NgFor, PrimeTemplate, InputText, ToggleSwitch, LocalizedTextInputComponent, TranslateModule]
})
export class AdminParkItemAccessConditionsTabComponent {
  @Input({ required: true }) formGroup!: FormGroup;
  @Input() accessConditionPresetOptions: Array<{ labelKey: string; value: AttractionAccessConditionType }> = [];
  @Input() accessConditionUnitOptions: Array<{ labelKey: string; value: AttractionAccessConditionUnit }> = [];
  @Input() selectedAccessConditionPreset: AttractionAccessConditionType = 'MinHeight';
  @Input() currentLang: string = 'en';
  @Input() isSaving: boolean = false;

  @Output() selectedAccessConditionPresetChange: EventEmitter<AttractionAccessConditionType> = new EventEmitter<AttractionAccessConditionType>();
  @Output() addAccessCondition: EventEmitter<AttractionAccessConditionType> = new EventEmitter<AttractionAccessConditionType>();
  @Output() removeAccessCondition: EventEmitter<number> = new EventEmitter<number>();
  @Output() moveAccessConditionUp: EventEmitter<number> = new EventEmitter<number>();
  @Output() moveAccessConditionDown: EventEmitter<number> = new EventEmitter<number>();
  @Output() accessConditionTypeChanged: EventEmitter<number> = new EventEmitter<number>();
  @Output() saveSection: EventEmitter<void> = new EventEmitter<void>();

  constructor(private readonly translateService: TranslateService) {
  }

  get accessConditions(): FormArray {
    return this.formGroup.get('accessConditions') as FormArray;
  }

  onSelectedAccessConditionPresetChange(value: AttractionAccessConditionType): void {
    this.selectedAccessConditionPresetChange.emit(value);
  }

  getAccessConditionTitle(index: number): string {
    const group: FormGroup = this.accessConditions.at(index) as FormGroup;
    const label: LocalizedItem<string>[] | null = this.toLocalizedItems(group.get('label')?.value);
    const resolvedLabel: string | undefined = resolveLocalizedValue(label ?? [], this.currentLang);

    if (resolvedLabel && resolvedLabel.trim().length > 0) {
      return resolvedLabel;
    }

    const type: AttractionAccessConditionType = group.get('type')?.value as AttractionAccessConditionType;
    return this.translateService.instant(this.getAccessConditionLabelKey(type));
  }

  private getAccessConditionLabelKey(type: AttractionAccessConditionType): string {
    switch (type) {
      case 'MinHeight':
        return 'admin.parks.items.accessConditionTypes.minHeight';
      case 'MinHeightAccompanied':
        return 'admin.parks.items.accessConditionTypes.minHeightAccompanied';
      case 'MaxHeight':
        return 'admin.parks.items.accessConditionTypes.maxHeight';
      case 'MinAge':
        return 'admin.parks.items.accessConditionTypes.minAge';
      case 'MinAgeAccompanied':
        return 'admin.parks.items.accessConditionTypes.minAgeAccompanied';
      case 'PregnancyRestriction':
        return 'admin.parks.items.accessConditionTypes.pregnancyRestriction';
      case 'HeartRestriction':
        return 'admin.parks.items.accessConditionTypes.heartRestriction';
      case 'BackNeckRestriction':
        return 'admin.parks.items.accessConditionTypes.backNeckRestriction';
      case 'WheelchairTransferRequired':
        return 'admin.parks.items.accessConditionTypes.wheelchairTransferRequired';
      case 'AccessPassRequired':
        return 'admin.parks.items.accessConditionTypes.accessPassRequired';
      case 'Custom':
      default:
        return 'admin.parks.items.accessConditionTypes.custom';
    }
  }

  private toLocalizedItems(value: unknown): LocalizedItem<string>[] | null {
    if (!Array.isArray(value)) {
      return null;
    }

    const items: LocalizedItem<string>[] = value
      .filter((item: unknown) => !!item && typeof item === 'object')
      .map((item: unknown) => {
        const candidate: Record<string, unknown> = item as Record<string, unknown>;
        return {
          languageCode: String(candidate['languageCode'] ?? ''),
          value: String(candidate['value'] ?? '')
        };
      });

    return items.length > 0 ? items : null;
  }
}
