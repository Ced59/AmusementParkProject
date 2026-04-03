import { Component, Input } from '@angular/core';
import { SearchResultItem } from '../../../models/search/search-result-item';
import { buildParkSlug } from '../../../commons/park-presentation.utils';

@Component({
  selector: 'app-search-result-card',
  templateUrl: './search-result-card.component.html',
  styleUrls: ['./search-result-card.component.scss'],
  standalone: false
})
export class SearchResultCardComponent {
  @Input() item: SearchResultItem | null = null;
  @Input() currentLang = 'en';

  get categoryLabelKey(): string {
    const category: string | undefined = this.item?.category?.trim();
    return category ? `home.categories.${category}` : 'home.categories.park';
  }

  get parkLink(): string[] | null {
    const parkId: string | null = this.extractParkId();
    const title: string | undefined = this.item?.title?.trim();

    if (!parkId || !title) {
      return null;
    }

    return ['/', this.currentLang, 'park', parkId, buildParkSlug(title)];
  }

  private extractParkId(): string | null {
    const originalId: string | undefined = this.item?.originalId?.trim();

    if (!originalId || !originalId.startsWith('park_')) {
      return null;
    }

    return originalId.substring(5) || null;
  }
}
