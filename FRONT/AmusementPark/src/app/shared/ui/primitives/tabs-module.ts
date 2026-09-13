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

import { TabList } from './tab-list';

import { Tab } from './tab';

import { TabPanels } from './tab-panels';

import { TabPanel } from './tab-panel';

import { Tabs } from './tabs';

@NgModule({ imports: [Tabs, TabList, Tab, TabPanels, TabPanel], exports: [Tabs, TabList, Tab, TabPanels, TabPanel] })
export class TabsModule {}
