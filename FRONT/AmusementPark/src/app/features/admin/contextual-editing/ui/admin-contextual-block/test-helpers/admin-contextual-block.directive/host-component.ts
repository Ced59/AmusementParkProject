import { Component } from '@angular/core';

import { AdminContextualBlockInstance } from '../../../../models/admin-contextual-block.model';

import { AdminContextualBlockDirective } from '../../admin-contextual-block.directive';

function createBlock(): AdminContextualBlockInstance {
  return {
    id: 'park.hero:park-1',
    type: 'park.hero',
    entityType: 'Park',
    entityId: 'park-1',
    contextLabel: 'Phantasialand',
    ids: { parkId: 'park-1' },
    labelKey: 'admin.contextualBlocks.blocks.parkHero.label',
    descriptionKey: 'admin.contextualBlocks.blocks.parkHero.description',
    iconClass: 'pi pi-image',
    capabilities: ['fullAdminEdit'],
    jsonScope: ['park.id'],
    localizedLanguageCodes: [],
    locationFallbackCenter: null,
    adminRoute: ['/', 'fr', 'admin', 'parks', 'edit', 'park-1'],
  };
}

export @Component({
  template: `
    <section [appAdminContextualBlock]="block">
      <a href="/fr/parks" (click)="$event.preventDefault()">Visitor link</a>
      <p>Visitor content</p>
    </section>
  `,
  imports: [AdminContextualBlockDirective],
})
class HostComponent {
  block: AdminContextualBlockInstance | null = createBlock();
}
