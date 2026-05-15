import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgFor, NgIf } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { InputText } from 'primeng/inputtext';

import { SelectOption } from '../models/select-option.model';

@Component({
  selector: 'app-park-items-filters',
  templateUrl: './park-items-filters.component.html',
  styleUrls: ['./park-items-filters.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgIf, NgFor, FormsModule, InputText, TranslateModule]
})
export class ParkItemsFiltersComponent {
  @Input({ required: true }) searchTerm!: Signal<string>;
  @Input({ required: true }) selectedCategory!: Signal<string | null>;
  @Input({ required: true }) selectedType!: Signal<string | null>;
  @Input({ required: true }) selectedZoneId!: Signal<string | null>;
  @Input({ required: true }) categoryOptions!: Signal<SelectOption[]>;
  @Input({ required: true }) typeOptions!: Signal<SelectOption[]>;
  @Input({ required: true }) zoneOptions!: Signal<SelectOption[]>;
  @Input({ required: true }) hasZones!: Signal<boolean>;

  @Output() searchChanged: EventEmitter<string> = new EventEmitter<string>();
  @Output() categoryChanged: EventEmitter<string | null> = new EventEmitter<string | null>();
  @Output() typeChanged: EventEmitter<string | null> = new EventEmitter<string | null>();
  @Output() zoneChanged: EventEmitter<string | null> = new EventEmitter<string | null>();

  onSearchChanged(value: string): void {
    this.searchChanged.emit(value ?? '');
  }

  onCategoryChanged(value: string | null): void {
    this.categoryChanged.emit(value);
  }

  onTypeChanged(value: string | null): void {
    this.typeChanged.emit(value);
  }

  onZoneChanged(value: string | null): void {
    this.zoneChanged.emit(value);
  }

  resolveOptionLabel(option: SelectOption | null | undefined, fallbackKey: string): string {
    if (!option) {
      return fallbackKey;
    }

    if (option.labelKey) {
      return option.labelKey;
    }

    return option.label ?? fallbackKey;
  }
}
