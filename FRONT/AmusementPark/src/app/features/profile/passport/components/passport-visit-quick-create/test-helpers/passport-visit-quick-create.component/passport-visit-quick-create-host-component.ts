import { Component } from '@angular/core';

import { PassportVisit } from '@app/models/passport/passport-visit.models';

import { PassportVisitQuickCreateComponent } from '../../passport-visit-quick-create.component';

export @Component({
  template: `
    <main class="app-layout-main">
      <app-passport-visit-quick-create
        [visible]="visible"
        (visitCreated)="createdVisit = $event">
      </app-passport-visit-quick-create>
    </main>
  `,
  imports: [PassportVisitQuickCreateComponent]
})
class PassportVisitQuickCreateHostComponent {
  visible: boolean = true;
  createdVisit: PassportVisit | null = null;
}
