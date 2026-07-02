import {
  AfterContentInit,
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ContentChildren,
  ElementRef,
  forwardRef,
  Input,
  QueryList,
  TemplateRef,
  ViewChild
} from '@angular/core';
import { NgIf, NgStyle, NgTemplateOutlet } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import Quill from 'quill';
import { UiTemplate } from './api';

@Component({
  selector: 'app-ui-editor',
  standalone: true,
  imports: [NgIf, NgStyle, NgTemplateOutlet],
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => Editor), multi: true }],
  template: `
    <div class="p-editor-container">
      <div #toolbar class="p-editor-toolbar">
        <ng-container *ngIf="template('header') as headerTemplate"><ng-container *ngTemplateOutlet="headerTemplate"></ng-container></ng-container>
      </div>
      <div #editor class="p-editor-content" [ngStyle]="style"></div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Editor implements AfterViewInit, AfterContentInit, ControlValueAccessor {
  @Input() readonly: boolean = false;
  @Input() style: Record<string, string> | null = null;
  @Input() placeholder: string | null = null;
  @ContentChildren(UiTemplate) templates!: QueryList<UiTemplate>;
  @ViewChild('editor') private editorElement?: ElementRef<HTMLElement>;
  @ViewChild('toolbar') private toolbarElement?: ElementRef<HTMLElement>;

  private editor: Quill | null = null;
  private pendingValue: string = '';
  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  ngAfterContentInit(): void {
  }

  ngAfterViewInit(): void {
    if (!this.editorElement || !this.toolbarElement) {
      return;
    }

    this.editor = new Quill(this.editorElement.nativeElement, {
      modules: { toolbar: this.toolbarElement.nativeElement },
      placeholder: this.placeholder ?? '',
      readOnly: this.readonly,
      theme: 'snow'
    });
    this.editor.root.innerHTML = this.pendingValue;
    this.editor.on('text-change', (): void => {
      this.onChange(this.editor?.root.innerHTML ?? '');
      this.onTouched();
    });
  }

  writeValue(value: string | null): void {
    this.pendingValue = value ?? '';
    if (this.editor) {
      this.editor.root.innerHTML = this.pendingValue;
    }
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.readonly = isDisabled;
    this.editor?.enable(!isDisabled);
  }

  template(name: string): TemplateRef<unknown> | null {
    return this.templates?.find((template: UiTemplate) => template.name === name)?.template ?? null;
  }
}
