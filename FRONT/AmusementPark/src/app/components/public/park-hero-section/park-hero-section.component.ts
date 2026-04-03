import { Component, Input } from '@angular/core';
import { ApiService } from '../../../services/api.service';
import { Park } from '../../../models/parks/park';
import { resolveLocalizedValue } from '../../../commons/localized-item.utils';
import { buildParkAddressLine, buildParkLocationLine } from '../../../commons/park-presentation.utils';

@Component({
  selector: 'app-park-hero-section',
  templateUrl: './park-hero-section.component.html',
  styleUrls: ['./park-hero-section.component.scss'],
  standalone: false
})
export class ParkHeroSectionComponent {
  @Input() park: Park | null = null;
  @Input() currentLang = 'en';

  constructor(private readonly apiService: ApiService) {
  }

  get logoUrl(): string | null {
    const imageId: string | undefined = this.park?.currentLogoImageId?.trim();
    return imageId ? this.apiService.buildImageUrl(imageId) : null;
  }

  get locationLine(): string | null {
    return buildParkLocationLine(this.park);
  }

  get addressLine(): string | null {
    return buildParkAddressLine(this.park);
  }

  get description(): string | null {
    return resolveLocalizedValue(this.park?.descriptions, this.currentLang) ?? null;
  }
}
