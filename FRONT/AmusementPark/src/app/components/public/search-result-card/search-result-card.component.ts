import { Component, Input } from '@angular/core';
import { SearchResultItem } from '@app/models/search/search-result-item';
import { buildParkSlug } from '@app/commons/park-presentation.utils';
import { NgIf } from '@angular/common';
import { Bind } from 'primeng/bind';
import { ButtonDirective } from 'primeng/button';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { getSearchCategoryTranslationKey } from '@shared/utils/display/display-label.helpers';

@Component({
    selector: 'app-search-result-card',
    templateUrl: './search-result-card.component.html',
    styleUrls: ['./search-result-card.component.scss'],
    imports: [NgIf, Bind, ButtonDirective, RouterLink, TranslateModule]
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
