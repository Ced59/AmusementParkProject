import { Component } from '@angular/core';

import { Select } from '../../primitives';

export @Component({
  standalone: true,
  imports: [Select],
  template: '<app-ui-select [options]="options" optionValue="value"></app-ui-select>'
})
class SelectHostComponent {
  readonly options: Array<{ labelKey: string; value: string }> = [
    { labelKey: 'admin.parks.types.themePark', value: 'ThemePark' }
  ];
}
