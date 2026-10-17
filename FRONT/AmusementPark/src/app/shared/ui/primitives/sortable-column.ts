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

@Directive({
  selector: '[appUiSortableColumn]',
  standalone: true
})
export class SortableColumn {
  @Input('appUiSortableColumn') field: string = '';
  @HostBinding('class.p-sortable-column') protected readonly sortableClass: boolean = true;
  @HostBinding('attr.tabindex') protected get tabIndex(): number {
    return this.field ? 0 : -1;
  }

  constructor(private readonly elementRef: ElementRef<HTMLElement>) {
  }

  @HostListener('keydown', ['$event'])
  onKeyDown(event: KeyboardEvent): void {
    if (!this.field || event.target !== this.elementRef.nativeElement || !['Enter', ' '].includes(event.key)) {
      return;
    }

    // Preserve the native column-header semantics and the table's existing
    // delegated click/sort contract; never activate a nested input twice.
    event.preventDefault();
    this.elementRef.nativeElement.click();
  }
}
