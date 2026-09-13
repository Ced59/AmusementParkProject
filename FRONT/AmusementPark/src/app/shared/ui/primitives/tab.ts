import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  HostBinding,
  HostListener,
  Input,
  NgModule,
  Output,
  signal,
  WritableSignal
} from '@angular/core';

import { NgIf } from '@angular/common';

import { Tabs } from './tabs';

@Component({
  selector: 'app-ui-tab',
  standalone: true,
  template: `<button type="button" class="p-tab-button" [disabled]="disabled" (click)="select($event)"><ng-content></ng-content></button>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Tab {
  @Input() value: string | number = 0;
  @Input() disabled: boolean = false;

  @HostBinding('class.p-tab') protected readonly tabClass: boolean = true;
  @HostBinding('class.p-tab-active') protected get activeClass(): boolean {
    return this.tabs.isValueActive(this.value);
  }

  constructor(private readonly tabs: Tabs) {
  }

  @HostListener('click', ['$event'])
  selectFromHost(event: MouseEvent): void {
    const target: EventTarget | null = event.target;
    if (target instanceof HTMLElement && target.closest('.p-tab-button')) {
      return;
    }

    this.select();
  }

  select(event?: MouseEvent): void {
    event?.stopPropagation();

    if (!this.disabled) {
      this.tabs.setValue(this.value);
    }
  }
}
