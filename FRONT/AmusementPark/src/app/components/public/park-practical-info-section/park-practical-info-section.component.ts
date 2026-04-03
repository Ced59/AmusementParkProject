import { Component, Input } from '@angular/core';
import { Park } from '../../../models/parks/park';
import { buildParkAddressLine } from '../../../commons/park-presentation.utils';

@Component({
  selector: 'app-park-practical-info-section',
  templateUrl: './park-practical-info-section.component.html',
  styleUrls: ['./park-practical-info-section.component.scss'],
  standalone: false
})
export class ParkPracticalInfoSectionComponent {
  @Input() park: Park | null = null;

  get addressLine(): string | null {
    return buildParkAddressLine(this.park);
  }
}
