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
  selector: 'app-ui-tab-panel',
  standalone: true,
  imports: [NgIf],
  template: `<ng-container *ngIf="isActive"><ng-content></ng-content></ng-container>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TabPanel {
  @Input() value: string | number = 0;
  @HostBinding('class.p-tabpanel') protected readonly tabPanelClass: boolean = true;

  constructor(protected readonly tabs: Tabs) {
  }

  get isActive(): boolean {
    return this.tabs.isValueActive(this.value);
  }
}
