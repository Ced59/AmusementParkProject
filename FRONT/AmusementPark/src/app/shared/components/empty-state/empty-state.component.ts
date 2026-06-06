import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { NgClass, NgIf } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-empty-state',
  templateUrl: './empty-state.component.html',
  styleUrls: ['./empty-state.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass, NgIf, TranslateModule]
})
export class EmptyStateComponent {
  @Input() iconClass: string = 'pi pi-inbox';
  @Input() titleKey: string | null = null;
  @Input() messageKey: string | null = null;
  @Input() variant: 'app' | 'public' = 'app';
  @Input() compact: boolean = false;
}
