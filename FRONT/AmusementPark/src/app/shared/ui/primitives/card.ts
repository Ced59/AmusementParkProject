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
  selector: 'app-ui-card',
  standalone: true,
  imports: [NgIf, NgTemplateOutlet],
  template: `
    <ng-container *ngIf="template('header') as headerTemplate">
      <div class="p-card-header">
        <ng-container *ngTemplateOutlet="headerTemplate"></ng-container>
      </div>
    </ng-container>
    <div *ngIf="!template('header') && (header || subheader)" class="p-card-header p-card-header--text">
      <div *ngIf="header" class="p-card-title">{{ header }}</div>
      <div *ngIf="subheader" class="p-card-subtitle">{{ subheader }}</div>
    </div>
    <div class="p-card-body">
      <div class="p-card-content">
        <ng-content></ng-content>
      </div>
    </div>
    <ng-container *ngIf="template('footer') as footerTemplate">
      <div class="p-card-footer">
        <ng-container *ngTemplateOutlet="footerTemplate"></ng-container>
      </div>
    </ng-container>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Card implements AfterContentInit {
  @Input() header: string | null = null;
  @Input() subheader: string | null = null;
  @Input() styleClass: string | null = null;
  @ContentChildren(UiTemplate) templates!: QueryList<UiTemplate>;

  @HostBinding('class') protected get hostClasses(): string {
    return `p-card ${this.styleClass ?? ''}`.trim();
  }

  ngAfterContentInit(): void {
  }

  template(name: string): TemplateRef<unknown> | null {
    return this.templates?.find((template: UiTemplate) => template.name === name)?.template ?? null;
  }
}

export { CardModule } from './card-module';
