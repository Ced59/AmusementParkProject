import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { Park } from '@app/models/parks/park';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { UiSelectOptionModel } from '@ui/forms';
import { ParkItemsMapViewModel } from '../models/park-items-map-view.model';
import { ParkItemsMapSectionComponent } from './park-items-map-section.component';

@Component({
  selector: 'app-park-map-view',
  templateUrl: './park-map-view.component.html',
  styleUrls: ['./park-map-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageStateComponent,
    RouterLink,
    TranslateModule,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective,
    PublicSharePanelComponent,
    ParkItemsMapSectionComponent
  ]
})
export class ParkMapViewComponent {
  @Input() state: ScreenState<unknown, string> | null = null;
  @Input() park: Park | null = null;
  @Input() map: ParkItemsMapViewModel | null = null;
  @Input() detailLink: string[] | null = null;
  @Input() itemsLink: string[] | null = null;
  @Input() selectedClosedFilter: string = 'openOnly';
  @Input() closedFilterOptions: UiSelectOptionModel[] = [];

  @Output() closedFilterChanged: EventEmitter<string | null> = new EventEmitter<string | null>();

  onClosedFilterSelectChanged(event: Event): void {
    const target: HTMLSelectElement | null = event.target instanceof HTMLSelectElement ? event.target : null;
    this.closedFilterChanged.emit(target?.value ?? null);
  }
}
