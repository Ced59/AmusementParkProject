import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { AttractionWaterExposureLevel } from '../../../../../../../models/parks/attraction-water-exposure-level';
import { EntitySelectOption } from '../../../../../../../models/shared/entity-select-option';

@Component({
  selector: 'app-admin-park-item-details-tab',
  templateUrl: './admin-park-item-details-tab.component.html',
  styleUrls: ['./admin-park-item-details-tab.component.scss']
})
export class AdminParkItemDetailsTabComponent {
  @Input({ required: true }) formGroup!: FormGroup;
  @Input() manufacturerOptions: EntitySelectOption[] = [];
  @Input() manufacturersLoading: boolean = false;
  @Input() manufacturerAddLink: any[] | string | null = null;
  @Input() manufacturerAddQueryParams: Record<string, string | number | boolean | null | undefined> | null = null;
  @Input() waterExposureLevelOptions: Array<{ labelKey: string; value: AttractionWaterExposureLevel }> = [];
  @Input() isSaving: boolean = false;

  @Output() saveSection: EventEmitter<void> = new EventEmitter<void>();
}
