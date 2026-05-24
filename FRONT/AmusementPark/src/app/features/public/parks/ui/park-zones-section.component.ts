import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { UiSectionHeaderComponent } from '@ui/primitives';
import { ParkZoneDetailViewModel } from '../models/park-zone-detail-view.model';

@Component({
  selector: 'app-park-zones-section',
  templateUrl: './park-zones-section.component.html',
  styleUrls: ['./park-zones-section.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule, UiSectionHeaderComponent]
})
export class ParkZonesSectionComponent {
  @Input() zones: ParkZoneDetailViewModel[] = [];
}
