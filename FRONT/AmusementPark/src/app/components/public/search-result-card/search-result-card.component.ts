import { Component, Input } from '@angular/core';
import { SearchResultItem } from '../../../models/search/search-result-item';

@Component({
  selector: 'app-search-result-card',
  templateUrl: './search-result-card.component.html',
  styleUrls: ['./search-result-card.component.scss']
})
export class SearchResultCardComponent {
  @Input() item: SearchResultItem | null = null;
  @Input() currentLang = 'en';

  get categoryLabelKey(): string {
    const category = this.item?.category?.trim();
    return category ? `home.categories.${category}` : 'home.categories.park';
  }

  get parkLink(): string[] | null {
    const parkId = this.extractParkId();
    const title = this.item?.title?.trim();

    if (!parkId || !title) {
      return null;
    }

    return ['/', this.currentLang, 'park', parkId, this.slugify(title)];
  }

  private extractParkId(): string | null {
    const originalId = this.item?.originalId?.trim();

    if (!originalId || !originalId.startsWith('park_')) {
      return null;
    }

    return originalId.substring(5) || null;
  }

  private slugify(text: string): string {
    return text
      .toLowerCase()
      .trim()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/(^-|-$)/g, '');
  }
}
