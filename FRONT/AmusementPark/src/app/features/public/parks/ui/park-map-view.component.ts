import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { Park } from '@app/models/parks/park';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSegmentedTab, UiSegmentedTabsComponent, UiSurfaceDirective } from '@ui/primitives';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { UiSelectOptionModel } from '@ui/forms';
import { ParkItemsMapViewModel } from '../models/park-items-map-view.model';
import { ParkItemsMapSectionComponent } from './park-items-map-section.component';
import { ParkLifecycleNoticeComponent } from './park-lifecycle-notice.component';
import { ParkOfficialMapViewModel, ParkMapPageTab } from '../models/park-official-map-view.model';
import { ParkOfficialMapsSectionComponent } from './park-official-maps-section.component';

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
    UiSegmentedTabsComponent,
    UiSurfaceDirective,
    PublicSharePanelComponent,
    ParkItemsMapSectionComponent,
    ParkLifecycleNoticeComponent,
    ParkOfficialMapsSectionComponent
  ]
})
export class ParkMapViewComponent {
  @Input() state: ScreenState<unknown, string> | null = null;
  @Input() park: Park | null = null;
  @Input() map: ParkItemsMapViewModel | null = null;
  @Input() officialMaps: readonly ParkOfficialMapViewModel[] = [];
  @Input() officialMapCount: number = 0;
  @Input() officialMapYears: readonly number[] = [];
  @Input() selectedOfficialMapYear: number | null = null;
  @Input() activeTab: ParkMapPageTab = 'interactive';
  @Input() detailLink: string[] | null = null;
  @Input() itemsLink: string[] | null = null;
  @Input() selectedClosedFilter: string = 'openOnly';
  @Input() closedFilterOptions: UiSelectOptionModel[] = [];

  @Output() closedFilterChanged: EventEmitter<string | null> = new EventEmitter<string | null>();
  @Output() tabSelected: EventEmitter<string> = new EventEmitter<string>();
  @Output() officialMapYearSelected: EventEmitter<number> = new EventEmitter<number>();

  onClosedFilterSelectChanged(event: Event): void {
    const target: HTMLSelectElement | null = event.target instanceof HTMLSelectElement ? event.target : null;
    this.closedFilterChanged.emit(target?.value ?? null);
  }

  protected mapTabs(): readonly UiSegmentedTab[] {
    return [
      {
        id: 'interactive',
        labelKey: 'parks.mapPage.tabs.interactive',
        count: this.map?.markers.length ?? 0
      },
      {
        id: 'official',
        labelKey: 'parks.mapPage.tabs.official',
        count: this.officialMapCount
      }
    ];
  }
}
