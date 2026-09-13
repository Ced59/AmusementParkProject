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
  selector: 'app-ui-panel',
  standalone: true,
  imports: [NgIf],
  template: `
    <div class="p-panel-header" (click)="toggle()" [class.p-panel-header--toggleable]="toggleable">
      <span>{{ header }}</span>
      <button *ngIf="toggleable" type="button" class="p-panel-toggle" [attr.aria-expanded]="!collapsed">
        <span [class]="collapsed ? 'pi pi-chevron-down' : 'pi pi-chevron-up'" aria-hidden="true"></span>
      </button>
    </div>
    <div class="p-panel-content" *ngIf="!collapsed">
      <ng-content></ng-content>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Panel {
  @Input() header: string | null = null;
  @Input() toggleable: boolean = false;
  @Input() collapsed: boolean = false;

  @HostBinding('class.p-panel') protected readonly panelClass: boolean = true;

  toggle(): void {
    if (this.toggleable) {
      this.collapsed = !this.collapsed;
    }
  }
}

export { PanelModule } from './panel-module';
