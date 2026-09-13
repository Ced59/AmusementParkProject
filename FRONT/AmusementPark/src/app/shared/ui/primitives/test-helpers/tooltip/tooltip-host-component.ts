import { Component } from '@angular/core';

import { Tooltip } from '../../primitives';

export @Component({
  standalone: true,
  imports: [Tooltip],
  template:
    '<button type="button" [appUiTooltip]="text" tooltipPosition="bottom">?</button>',
})
class TooltipHostComponent {
  text = 'Helpful details';
}
