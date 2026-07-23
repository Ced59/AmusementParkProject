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

@Component({
  selector: 'app-ui-tab-list',
  standalone: true,
  template: `<div class="p-tablist-tab-list"><ng-content></ng-content></div>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TabList {
  @HostBinding('class.p-tablist') protected readonly tabListClass: boolean = true;
}

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

@Component({
  selector: 'app-ui-tab-panels',
  standalone: true,
  template: `<ng-content></ng-content>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TabPanels {
  @HostBinding('class.p-tabpanels') protected readonly tabPanelsClass: boolean = true;
}

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

@NgModule({ imports: [Tabs, TabList, Tab, TabPanels, TabPanel], exports: [Tabs, TabList, Tab, TabPanels, TabPanel] })
export class TabsModule {}
