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
  selector: 'app-ui-tag',
  standalone: true,
  template: `<span class="p-tag-value">{{ value }}</span><ng-content></ng-content>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Tag {
  @Input() value: string | number | null = null;
  @Input() severity: string | null = null;

  @HostBinding('class') protected get hostClasses(): string {
    return ['p-tag', this.severity ? `p-tag-${this.severity}` : null].filter((value: string | null) => value !== null).join(' ');
  }
}

export { TagModule } from './tag-module';
