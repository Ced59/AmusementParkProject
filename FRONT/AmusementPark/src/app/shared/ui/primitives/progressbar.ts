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
  selector: 'app-ui-progress-bar',
  standalone: true,
  template: `<div class="p-progressbar-value" [style.width.%]="normalizedValue"></div>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProgressBar {
  @Input() value: number | null = 0;

  @HostBinding('class.p-progressbar') protected readonly progressClass: boolean = true;

  protected get normalizedValue(): number {
    const value: number = Number(this.value ?? 0);
    return Math.max(0, Math.min(100, Number.isFinite(value) ? value : 0));
  }
}
