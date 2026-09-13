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

@Component({
  selector: 'app-ui-progress-spinner',
  standalone: true,
  template: `<span class="pi pi-spin pi-spinner" aria-hidden="true"></span>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProgressSpinner {
  @Input() styleClass: string | null = null;

  @HostBinding('class') protected get hostClasses(): string {
    return `p-progress-spinner ${this.styleClass ?? ''}`.trim();
  }
}

export { ProgressSpinnerModule } from './progress-spinner-module';
