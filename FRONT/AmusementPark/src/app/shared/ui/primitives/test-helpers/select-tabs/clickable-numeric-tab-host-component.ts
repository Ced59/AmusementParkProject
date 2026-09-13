import { Component } from '@angular/core';

import { Tab, TabList, TabPanel, TabPanels, Tabs } from '../../primitives';

export @Component({
  standalone: true,
  imports: [Tabs, TabList, Tab, TabPanels, TabPanel],
  template: `
    <app-ui-tabs [value]="activeTab" (valueChange)="activeTab = $event">
      <app-ui-tab-list>
        <app-ui-tab value="1">Location</app-ui-tab>
      </app-ui-tab-list>
      <app-ui-tab-panels>
        <app-ui-tab-panel value="1">
          @if (activeTab === 1) {
            <span class="active-panel">Location panel</span>
          }
        </app-ui-tab-panel>
      </app-ui-tab-panels>
    </app-ui-tabs>
  `
})
class ClickableNumericTabHostComponent {
  activeTab: string | number = 0;
}
