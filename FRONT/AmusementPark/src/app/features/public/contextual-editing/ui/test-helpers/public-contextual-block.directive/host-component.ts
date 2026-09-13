import { Component } from '@angular/core';

import { PublicContextualBlockMarker } from '../../../models/public-contextual-block-marker.model';

import { PublicContextualBlockDirective } from '../../public-contextual-block.directive';

export @Component({
  template: '<section [appPublicContextualBlock]="marker">Public block</section>',
  imports: [PublicContextualBlockDirective]
})
class HostComponent {
  marker: PublicContextualBlockMarker | null = {
    type: 'park.description',
    parkId: 'park-1',
    contextLabel: 'Phantasialand',
    languageCode: 'fr'
  };
}
