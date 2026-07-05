import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { EmptyStateComponent } from '@shared/components/empty-state/empty-state.component';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { SafeRichHtmlPipe } from '@shared/pipes';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { ParkZonesPageViewModel } from '../models/park-zone-page.model';

@Component({
  selector: 'app-park-zones-view',
  templateUrl: './park-zones-view.component.html',
  styleUrls: ['./park-zones-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NgFor,
    NgIf,
    RouterLink,
    TranslateModule,
    EmptyStateComponent,
    PageStateComponent,
    SafeRichHtmlPipe,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    PublicSharePanelComponent,
    UiSurfaceDirective
  ]
})
export class ParkZonesViewComponent {
  @Input({ required: true }) state!: Signal<ScreenState<unknown, string>>;
  @Input({ required: true }) page!: Signal<ParkZonesPageViewModel | null>;
  @Output() backClicked: EventEmitter<void> = new EventEmitter<void>();

  onBackClicked(): void {
    this.backClicked.emit();
  }
}
