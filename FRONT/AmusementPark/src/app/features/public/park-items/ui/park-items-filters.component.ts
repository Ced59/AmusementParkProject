import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal, computed } from '@angular/core';

import { SelectOption } from '../models/select-option.model';
import { UiSearchPanelComponent, UiSearchPanelSelectFilterModel } from '@ui/forms';

@Component({
  selector: 'app-park-items-filters',
  templateUrl: './park-items-filters.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [UiSearchPanelComponent]
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

  protected readonly filters = computed<UiSearchPanelSelectFilterModel[]>(() => [
    {
      id: 'category',
      labelKey: 'parkItems.filters.category',
      selectedValue: this.selectedCategory(),
      options: this.categoryOptions()
    },
    {
      id: 'type',
      labelKey: 'parkItems.filters.type',
      selectedValue: this.selectedType(),
      options: this.typeOptions()
    },
    {
      id: 'zone',
      labelKey: 'parkItems.filters.zone',
      selectedValue: this.selectedZoneId(),
      options: this.zoneOptions(),
      hidden: !this.hasZones()
    }
  ]);

  onSearchChanged(value: string): void {
    this.searchChanged.emit(value ?? '');
  }

  onFilterChanged(event: { id: string; value: string | null }): void {
    if (event.id === 'category') {
      this.categoryChanged.emit(event.value);
      return;
    }

    if (event.id === 'type') {
      this.typeChanged.emit(event.value);
      return;
    }

    if (event.id === 'zone') {
      this.zoneChanged.emit(event.value);
    }
  }
}
