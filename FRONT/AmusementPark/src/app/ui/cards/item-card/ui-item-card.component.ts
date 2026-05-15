import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ParkItemCardViewModel } from '@features/public/park-items/models/park-item-card.model';
import { UiButtonDirective, UiChipComponent } from '@ui/primitives';

@Component({
  selector: 'app-ui-item-card',
  templateUrl: './ui-item-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule, UiButtonDirective, UiChipComponent]
})
export class UiItemCardComponent {
  @Input() card: ParkItemCardViewModel | null = null;
  @Input() actionLabelKey: string = 'parkItems.actions.viewDetails';
}
