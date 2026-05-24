import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { UiButtonDirective, UiKickerComponent } from '@ui/primitives';
import { UiFieldInputComponent } from '../field-input/ui-field-input.component';
import { UiSearchPanelSelectFilterModel, UiSelectOptionModel } from '../models/ui-search-panel.model';

let nextUiSearchPanelId: number = 0;

@Component({
  selector: 'app-ui-search-panel',
  templateUrl: './ui-search-panel.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, TranslateModule, UiButtonDirective, UiKickerComponent, UiFieldInputComponent]
})
export class UiSearchPanelComponent {
  @Input() panelId: string | null = null;
  @Input() kickerLabelKey: string | null = null;
  @Input() kickerText: string | null = null;
  @Input() kickerIconClass: string = 'pi pi-search';
  @Input() titleKey: string | null = null;
  @Input() titleText: string | null = null;
  @Input() searchLabelKey: string | null = null;
  @Input() searchLabelText: string | null = null;
  @Input() searchPlaceholderKey: string | null = null;
  @Input() searchPlaceholderText: string | null = null;
  @Input() searchTerm: string = '';
  @Input() searchInputId: string = `ui-search-${nextUiSearchPanelId++}`;
  @Input() filters: UiSearchPanelSelectFilterModel[] = [];
  @Input() clearActionLabelKey: string = 'parks.search.clear';
  @Input() showClearAction: boolean = true;

  @Output() searchTermChange: EventEmitter<string> = new EventEmitter<string>();
  @Output() filterChanged: EventEmitter<{ id: string; value: string | null }> = new EventEmitter<{ id: string; value: string | null }>();
  @Output() clearClicked: EventEmitter<void> = new EventEmitter<void>();

  protected get hasHeader(): boolean {
    return !!this.kickerLabelKey || !!this.kickerText || !!this.titleKey || !!this.titleText;
  }

  protected get hasTitle(): boolean {
    return !!this.titleKey || !!this.titleText;
  }

  protected get visibleFilters(): UiSearchPanelSelectFilterModel[] {
    return this.filters.filter((filter: UiSearchPanelSelectFilterModel) => !filter.hidden);
  }

  protected onSearchTermChanged(value: string): void {
    this.searchTermChange.emit(value ?? '');
  }

  protected onFilterChanged(filterId: string, value: string | null): void {
    this.filterChanged.emit({ id: filterId, value });
  }

  protected clear(): void {
    this.clearClicked.emit();
  }

  protected resolveOptionLabel(option: UiSelectOptionModel | null | undefined): string {
    if (!option) {
      return '';
    }

    if (option.labelKey) {
      return option.labelKey;
    }

    return option.label ?? '';
  }
}
