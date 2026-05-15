import { Component, Input } from '@angular/core';
import { NgIf } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { SafeExternalUrlPipe } from '@shared/pipes';

import { ParkDetailViewModel } from '../models/park-detail-view.model';

@Component({
    selector: 'app-park-practical-info-section',
    templateUrl: './park-practical-info-section.component.html',
    styleUrls: ['./park-practical-info-section.component.scss'],
    imports: [NgIf, TranslateModule, SafeExternalUrlPipe]
})
export class ParkPracticalInfoSectionComponent {
  @Input() park: ParkDetailViewModel | null = null;
}
