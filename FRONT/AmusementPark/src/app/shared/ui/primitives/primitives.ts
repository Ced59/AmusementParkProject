import {
  AfterContentInit,
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ContentChildren,
  Directive,
  ElementRef,
  EventEmitter,
  forwardRef,
  HostBinding,
  HostListener,
  Input,
  NgModule,
  OnChanges,
  OnDestroy,
  Output,
  QueryList,
  Renderer2,
  TemplateRef,
} from '@angular/core';
import { FormsModule, NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';
import { NgClass, NgFor, NgIf, NgStyle, NgTemplateOutlet } from '@angular/common';
import { TranslateService } from '@ngx-translate/core';
import { UiTemplate } from './ui-template';
export { MessageService, UiTemplate, ToastMessage } from './api';
export { Tab, TabList, TabPanel, TabPanels, Tabs, TabsModule } from './tabs';

export interface PaginatorState {
  page?: number;
  first?: number;
  rows?: number;
  pageCount?: number;
}

export interface TableLazyLoadEvent {
  first?: number;
  rows?: number;
  sortField?: string | string[] | null;
  sortOrder?: number | null;
}

export interface TableSortEvent {
  field?: string | string[] | null;
  order?: number | null;
}

type TooltipPosition = 'top' | 'right' | 'bottom' | 'left';

let nextTooltipId = 0;

export { Bind } from './bind';
export { Ripple } from './ripple';
export { InputText } from './inputtext';
export { Tooltip } from './tooltip';
export { ButtonDirective } from './button-directive';
export { Card } from './card';
export { Tag } from './tag';
export { ProgressSpinner } from './progressspinner';
export { ProgressBar } from './progressbar';
export { Panel } from './panel';
export { Paginator } from './paginator';
export { Table } from './table';
export { SortIcon } from './sort-icon';
export { SortableColumn } from './sortable-column';
export { Select } from './select';
export { Checkbox } from './checkbox';
export { ToggleSwitch } from './toggleswitch';
export { InputNumber } from './inputnumber';
export { Dialog } from './dialog';
export { ButtonModule } from './button-module';
export { CardModule } from './card-module';
export { InputTextModule } from './input-text-module';
export { SelectModule } from './select-module';
export { PaginatorModule } from './paginator-module';
export { TableModule } from './table-module';
export { TagModule } from './tag-module';
export { ToggleSwitchModule } from './toggle-switch-module';
export { TooltipModule } from './tooltip-module';
export { ProgressSpinnerModule } from './progress-spinner-module';
export { PanelModule } from './panel-module';
export { DividerModule } from './divider-module';
