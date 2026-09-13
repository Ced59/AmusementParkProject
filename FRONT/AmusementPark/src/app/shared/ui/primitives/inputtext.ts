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
  selector: 'input[appUiInputText], textarea[appUiInputText]',
  standalone: true
})
export class InputText {
  @HostBinding('class.p-inputtext') protected readonly inputClass: boolean = true;
  @HostBinding('class.app-input') protected readonly appInputClass: boolean = true;
}

export { InputTextModule } from './input-text-module';
