import { Component } from '@angular/core';

import { PublicContextualBlockMarker } from '@features/public/contextual-editing/models/public-contextual-block-marker.model';

import { PublicContextualBlockDirective } from '@features/public/contextual-editing/ui/public-contextual-block.directive';

export @Component({
  template: `
    <section [appPublicContextualBlock]="marker">
      <a href="/fr/parks" (click)="$event.preventDefault()">Visitor link</a>
      <p>Visitor content</p>
    </section>
  `,
  imports: [PublicContextualBlockDirective],
})
class HostComponent {
  marker: PublicContextualBlockMarker = {
    type: 'park.description',
    parkId: 'park-1',
    contextLabel: 'Phantasialand',
    languageCode: 'fr',
  };
}
