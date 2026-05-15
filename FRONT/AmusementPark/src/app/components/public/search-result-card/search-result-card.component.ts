import { Component, Input } from '@angular/core';

import { SearchResultItem } from '@app/models/search/search-result-item';
import { buildParkSlug } from '@shared/utils/display/park-presentation.helpers';
import { getSearchCategoryTranslationKey } from '@shared/utils/display/display-label.helpers';
import { UiResultCardComponent } from '@ui/cards';

@Component({
  selector: 'app-search-result-card',
  templateUrl: './search-result-card.component.html',
  imports: [UiResultCardComponent]
})
export class SearchResultCardComponent {
  @Input() item: SearchResultItem | null = null;
  @Input() currentLang = 'en';

  get categoryLabelKey(): string {
    return getSearchCategoryTranslationKey(this.item?.category);
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
