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
  selector: 'app-ui-tabs',
  standalone: true,
  template: `<ng-content></ng-content>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Tabs {
  private readonly valueSignal: WritableSignal<string | number> = signal(0);

  @Input()
  set value(value: string | number) {
    this.valueSignal.set(value);
  }

  get value(): string | number {
    return this.valueSignal();
  }

  @Output() valueChange: EventEmitter<string | number> = new EventEmitter<string | number>();

  @HostBinding('class.p-tabs') protected readonly tabsClass: boolean = true;

  setValue(value: string | number): void {
    const normalizedValue: string | number = this.normalizeSelectedValue(value);
    this.valueSignal.set(normalizedValue);
    this.valueChange.emit(normalizedValue);
  }

  isValueActive(value: string | number): boolean {
    return this.normalizeValue(this.value) === this.normalizeValue(value);
  }

  private normalizeSelectedValue(value: string | number): string | number {
    if (typeof this.value === 'number') {
      const numericValue: number = typeof value === 'number' ? value : Number(value);
      return Number.isFinite(numericValue) ? numericValue : value;
    }

    return String(value);
  }

  private normalizeValue(value: string | number): string {
    return String(value);
  }
}

export { TabList } from './tab-list';
export { Tab } from './tab';
export { TabPanels } from './tab-panels';
export { TabPanel } from './tab-panel';
export { TabsModule } from './tabs-module';
