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
  selector: 'app-ui-tab-list',
  standalone: true,
  template: `<div class="p-tablist-tab-list"><ng-content></ng-content></div>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TabList {
  @HostBinding('class.p-tablist') protected readonly tabListClass: boolean = true;
}
