import { Component, EventEmitter, Input, Output } from '@angular/core';
import { NgClass } from '@angular/common';
import { Bind } from 'primeng/bind';
import { ButtonDirective } from 'primeng/button';

@Component({
    selector: 'app-editor-save-toolbar',
    templateUrl: './editor-save-toolbar.component.html',
    styleUrls: ['./editor-save-toolbar.component.scss'],
    imports: [NgClass, Bind, ButtonDirective]
})
export class EditorSaveToolbarComponent {
  @Input() statusLabel: string = '';
  @Input() isDirty: boolean = false;
  @Input() isSaving: boolean = false;
  @Input() backButtonLabel: string = '';
  @Input() saveAllButtonLabel: string = '';
  @Input() saveAndCloseButtonLabel: string = '';
  @Input() showBackButton: boolean = true;
  @Input() showSaveAndCloseButton: boolean = true;
  @Input() backIcon: string = 'pi pi-list';
  @Input() saveAllIcon: string = 'pi pi-save';
  @Input() saveAndCloseIcon: string = 'pi pi-check';

  @Output() back: EventEmitter<void> = new EventEmitter<void>();
  @Output() saveAll: EventEmitter<void> = new EventEmitter<void>();
  @Output() saveAndClose: EventEmitter<void> = new EventEmitter<void>();
}
