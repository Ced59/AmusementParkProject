import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { SafeRichHtmlPipe } from '@shared/pipes';
import { UiPhotoCarouselComponent } from '@ui/media';
import { UiButtonDirective, UiChipComponent, UiKickerComponent } from '@ui/primitives';
import { ParkReferenceDetailViewModel } from '../models/park-reference-detail-view.model';

@Component({
  selector: 'app-park-reference-detail-view',
  templateUrl: './park-reference-detail-view.component.html',
  styleUrls: ['./park-reference-detail-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageStateComponent,
    PaginationComponent,
    RouterLink,
    TranslateModule,
    SafeRichHtmlPipe,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiPhotoCarouselComponent
  ]
})
export class ParkReferenceDetailViewComponent {
  @Input() state!: Signal<ScreenState<unknown, string>>;
  @Input() reference!: Signal<ParkReferenceDetailViewModel | null>;
  @Input() attractionsLoading!: Signal<boolean>;

  @Output() backClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() attractionsPageChanged: EventEmitter<{ page?: number; rows?: number }> = new EventEmitter<{ page?: number; rows?: number }>();

  goBack(): void {
    this.backClicked.emit();
  }

  onAttractionsPageChanged(event: { page?: number; rows?: number }): void {
    this.attractionsPageChanged.emit(event);
  }
}
