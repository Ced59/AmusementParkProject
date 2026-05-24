import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-ui-map-slot',
  templateUrl: './ui-map-slot.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule]
})
export class UiMapSlotComponent {
  @Input() available: boolean = true;
  @Input() placeholderTitleKey: string = 'maps.placeholder.title';
  @Input() placeholderMessageKey: string = 'maps.placeholder.message';
  @Input() minHeight: string | null = null;
}
