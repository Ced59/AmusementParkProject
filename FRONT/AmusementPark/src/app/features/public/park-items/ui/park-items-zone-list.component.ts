import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';

import { ParkItemZoneCardViewModel } from '../models/park-items-page-view.model';

@Component({
  selector: 'app-park-items-zone-list',
  templateUrl: './park-items-zone-list.component.html',
  styleUrls: ['./park-items-zone-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgFor, NgIf, TranslateModule, ButtonDirective]
})
export class ParkItemsZoneListComponent {
  @Input({ required: true }) zoneCards!: Signal<ParkItemZoneCardViewModel[]>;

  @Output() zoneSelected: EventEmitter<string | null> = new EventEmitter<string | null>();

  selectZone(zoneId: string | null): void {
    this.zoneSelected.emit(zoneId);
  }
}
