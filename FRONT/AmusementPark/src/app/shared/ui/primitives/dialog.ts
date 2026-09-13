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
  selector: 'app-ui-dialog',
  standalone: true,
  imports: [NgIf, NgStyle, NgClass, NgTemplateOutlet],
  template: `
    <div *ngIf="visible" class="p-dialog-mask" [class.p-dialog-mask--modal]="modal" (click)="onMaskClick($event)">
      <section class="p-dialog" [ngClass]="styleClass" [ngStyle]="style" role="dialog" aria-modal="true">
        <header *ngIf="showHeader" class="p-dialog-header">
          <span class="p-dialog-title">{{ header }}</span>
          <button *ngIf="closable" type="button" class="p-dialog-header-close" (click)="hide()"><span class="pi pi-times" aria-hidden="true"></span></button>
        </header>
        <div class="p-dialog-content"><ng-content></ng-content></div>
        <ng-container *ngIf="template('footer') as footerTemplate">
          <footer class="p-dialog-footer"><ng-container *ngTemplateOutlet="footerTemplate"></ng-container></footer>
        </ng-container>
      </section>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Dialog implements AfterContentInit {
  @Input() visible: boolean = false;
  @Input() header: string | null = null;
  @Input() modal: boolean = false;
  @Input() dismissableMask: boolean = false;
  @Input() closable: boolean = true;
  @Input() draggable: boolean = false;
  @Input() resizable: boolean = false;
  @Input() showHeader: boolean = true;
  @Input() style: Record<string, string> | null = null;
  @Input() styleClass: string | null = null;
  @Output() visibleChange: EventEmitter<boolean> = new EventEmitter<boolean>();
  @Output() onHide: EventEmitter<void> = new EventEmitter<void>();
  @ContentChildren(UiTemplate) templates!: QueryList<UiTemplate>;

  ngAfterContentInit(): void {
  }

  hide(): void {
    this.visible = false;
    this.visibleChange.emit(false);
    this.onHide.emit();
  }

  onMaskClick(event: Event): void {
    if (this.dismissableMask && event.target instanceof HTMLElement && event.target.classList.contains('p-dialog-mask')) {
      this.hide();
    }
  }

  template(name: string): TemplateRef<unknown> | null {
    return this.templates?.find((template: UiTemplate) => template.name === name)?.template ?? null;
  }
}
