import { Component, Input } from '@angular/core';
import { Park } from '@app/models/parks/park';
import { buildParkAddressLine } from '@app/commons/park-presentation.utils';
import { NgIf } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-park-practical-info-section',
    templateUrl: './park-practical-info-section.component.html',
    styleUrls: ['./park-practical-info-section.component.scss'],
    imports: [NgIf, TranslateModule]
})
export class ParkPracticalInfoSectionComponent {
  @Input() park: Park | null = null;

  get addressLine(): string | null {
    return buildParkAddressLine(this.park);
  }
}
