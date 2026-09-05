import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { UiButtonDirective, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { ParkOfficialMapViewModel } from '../models/park-official-map-view.model';

@Component({
  selector: 'app-park-official-maps-section',
  templateUrl: './park-official-maps-section.component.html',
  styleUrls: ['./park-official-maps-section.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule, UiButtonDirective, UiKickerComponent, UiSurfaceDirective]
})
export class ParkOfficialMapsSectionComponent {
  @Input() parkName: string = '';
  @Input() maps: readonly ParkOfficialMapViewModel[] = [];
  @Input() years: readonly number[] = [];
  @Input() selectedYear: number | null = null;

  @Output() yearSelected: EventEmitter<number> = new EventEmitter<number>();

  onYearChanged(event: Event): void {
    const target: HTMLSelectElement | null = event.target instanceof HTMLSelectElement ? event.target : null;
    const year: number = Number(target?.value);
    if (Number.isInteger(year)) {
      this.yearSelected.emit(year);
    }
  }

  protected formatLabelKey(map: ParkOfficialMapViewModel): string {
    return `parks.mapPage.official.formats.${map.format.toLowerCase()}`;
  }
}
