import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { SafeRichHtmlPipe } from '@shared/pipes';
import { UiButtonDirective, UiChipComponent, UiKickerComponent } from '@ui/primitives';
import { UiPrimitiveTone } from '@ui/primitives/models/ui-primitive-variant.model';

@Component({
  selector: 'app-ui-result-card',
  templateUrl: './ui-result-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule, SafeRichHtmlPipe, UiButtonDirective, UiChipComponent, UiKickerComponent]
})
export class UiResultCardComponent {
  @Input() title: string | null = null;
  @Input() description: string | null = null;
  @Input() kickerLabelKey: string | null = null;
  @Input() kickerText: string | null = null;
  @Input() kickerIconClass: string = 'pi pi-search';
  @Input() iconClass: string = 'pi pi-search';
  @Input() kickerTone: UiPrimitiveTone = 'primary';
  @Input() routerLink: string[] | null = null;
  @Input() actionLabelKey: string = 'home.search.openResult';
  @Input() badgeLabelKey: string | null = null;
  @Input() badgeText: string | null = null;
  @Input() badgeTone: UiPrimitiveTone = 'soft';

  protected get hasKicker(): boolean {
    return !!this.kickerLabelKey || !!this.kickerText;
  }

  protected get hasBadge(): boolean {
    return !!this.badgeLabelKey || !!this.badgeText;
  }
}
