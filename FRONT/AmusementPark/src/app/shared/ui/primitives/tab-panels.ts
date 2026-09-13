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

@Component({
  selector: 'app-ui-tab-panels',
  standalone: true,
  template: `<ng-content></ng-content>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TabPanels {
  @HostBinding('class.p-tabpanels') protected readonly tabPanelsClass: boolean = true;
}
