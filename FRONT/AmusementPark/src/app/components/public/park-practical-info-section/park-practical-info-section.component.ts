import { Component, Input } from '@angular/core';
import { Park } from '../../../models/parks/park';

@Component({
  selector: 'app-park-practical-info-section',
  templateUrl: './park-practical-info-section.component.html',
  styleUrls: ['./park-practical-info-section.component.scss']
})
export class ParkPracticalInfoSectionComponent {
  @Input() park: Park | null = null;

  get addressLine(): string | null {
    const parts = [this.park?.street, this.park?.postalCode, this.park?.city]
      .filter((part: string | undefined): part is string => !!part?.trim());

    return parts.length > 0 ? parts.join(', ') : null;
  }

  get hasContent(): boolean {
    return !!this.addressLine || !!this.park?.city || !!this.park?.webSiteUrl || !!this.park?.countryCode;
  }
}
