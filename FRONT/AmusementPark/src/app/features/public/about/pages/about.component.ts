import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSectionHeaderComponent, UiSurfaceDirective } from '@ui/primitives';

@Component({
  selector: 'app-about',
  templateUrl: './about.component.html',
  styleUrl: './about.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    TranslateModule,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSectionHeaderComponent,
    UiSurfaceDirective
  ]
})
export class AboutComponent {
}
