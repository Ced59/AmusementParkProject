import { Component, Input } from '@angular/core';
import { ApiService } from '../../../services/api.service';
import { Park } from '../../../models/parks/park';

@Component({
  selector: 'app-park-hero-section',
  templateUrl: './park-hero-section.component.html',
  styleUrls: ['./park-hero-section.component.scss']
})
export class ParkHeroSectionComponent {
  @Input() park: Park | null = null;

  constructor(private readonly apiService: ApiService) {
  }

  get logoUrl(): string | null {
    const imageId = this.park?.currentLogoImageId?.trim();
    return imageId ? this.apiService.buildImageUrl(imageId) : null;
  }

  get locationLine(): string | null {
    const parts = [this.park?.city, this.park?.countryCode]
      .filter((part: string | undefined): part is string => !!part?.trim());

    return parts.length > 0 ? parts.join(' · ') : null;
  }

  get addressLine(): string | null {
    const parts = [this.park?.street, this.park?.postalCode, this.park?.city]
      .filter((part: string | undefined): part is string => !!part?.trim());

    return parts.length > 0 ? parts.join(', ') : null;
  }
}
