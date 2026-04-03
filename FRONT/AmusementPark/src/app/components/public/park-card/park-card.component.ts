import { Component, Input } from '@angular/core';
import { ApiService } from '../../../services/api.service';
import { Park } from '../../../models/parks/park';

@Component({
  selector: 'app-park-card',
  templateUrl: './park-card.component.html',
  styleUrls: ['./park-card.component.scss']
})
export class ParkCardComponent {
  @Input() park: Park | null = null;
  @Input() currentLang = 'en';
  @Input() compact = false;

  constructor(private readonly apiService: ApiService) {
  }

  get parkLink(): string[] | null {
    if (!this.park?.id || !this.park?.name) {
      return null;
    }

    return ['/', this.currentLang, 'park', this.park.id, this.slugify(this.park.name)];
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

  get coordinatesLine(): string | null {
    if (!this.park) {
      return null;
    }

    return `${this.park.latitude.toFixed(3)}, ${this.park.longitude.toFixed(3)}`;
  }

  private slugify(text: string): string {
    return text
      .toLowerCase()
      .trim()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/(^-|-$)/g, '');
  }
}
