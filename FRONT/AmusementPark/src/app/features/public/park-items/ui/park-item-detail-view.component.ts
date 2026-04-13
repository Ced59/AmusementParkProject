import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';

import { PageStateComponent } from '@app/components/shared/page-state/page-state.component';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { ParkItemDetailViewModel } from '../models/park-item-detail-view.model';

@Component({
  selector: 'app-park-item-detail-view',
  templateUrl: './park-item-detail-view.component.html',
  styleUrls: ['./park-item-detail-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgIf, NgFor, RouterLink, PageStateComponent, TranslateModule, ButtonDirective]
})
export class ParkItemDetailViewComponent {
  @Input({ required: true }) state!: Signal<ScreenState<unknown, string>>;
  @Input({ required: true }) detail!: Signal<ParkItemDetailViewModel | null>;

  @Output() backToItemsClicked: EventEmitter<void> = new EventEmitter<void>();

  goBackToItems(): void {
    this.backToItemsClicked.emit();
  }
}
