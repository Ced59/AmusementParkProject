import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ImageDisplayComponent } from '@app/components/shared/image-display/image-display.component';
import { SearchResultItem } from '@app/models/search/search-result-item';
import { buildParkSlug } from '@shared/utils/display/park-presentation.helpers';
import { getSearchCategoryTranslationKey } from '@shared/utils/display/display-label.helpers';
import { SafeRichHtmlPipe } from '@shared/pipes';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { UiButtonDirective } from '@ui/primitives';
import { UiPrimitiveTone } from '@ui/primitives/models/ui-primitive-variant.model';

@Component({
  selector: 'app-search-result-card',
  templateUrl: './search-result-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ImageDisplayComponent, RouterLink, TranslateModule, SafeRichHtmlPipe, UiButtonDirective]
})
export class SearchResultCardComponent {
  constructor(private readonly countryDisplayService: CountryDisplayService) {
  }

  @Input() item: SearchResultItem | null = null;
  @Input() currentLang = 'en';

  get categoryLabelKey(): string {
    return getSearchCategoryTranslationKey(this.item?.category);
  }

  get cardIconClass(): string {
    const category: string = this.normalizedCategory;

    if (category === 'attraction' || category.includes('item')) {
      return 'pi pi-bolt';
    }

    if (category === 'hotel') {
      return 'pi pi-home';
    }

    if (category === 'restaurant') {
      return 'pi pi-shop';
    }

    if (category === 'shop') {
      return 'pi pi-shopping-bag';
    }

    if (category === 'show') {
      return 'pi pi-star';
    }

    if (category === 'operator') {
      return 'pi pi-building';
    }

    if (category === 'manufacturer') {
      return 'pi pi-wrench';
    }

    if (category === 'founder') {
      return 'pi pi-user';
    }

    return 'pi pi-map';
  }

  get cardTone(): UiPrimitiveTone {
    const category: string = this.normalizedCategory;

    if (category === 'attraction' || category.includes('item')) {
      return 'sky';
    }

    if (category === 'hotel' || category === 'restaurant') {
      return 'gold';
    }

    if (category === 'shop' || category === 'show') {
      return 'rose';
    }

    if (category === 'operator') {
      return 'lime';
    }

    if (category === 'manufacturer') {
      return 'purple';
    }

    return 'primary';
  }

  get toneClass(): string {
    return `app-search-result-card--${this.cardTone}`;
  }

  get detailLink(): string[] | null {
    const currentItem: SearchResultItem | null = this.item;
    if (!currentItem) {
      return null;
    }

    const originalId: string = currentItem.originalId?.trim() ?? '';
    const title: string = currentItem.title?.trim() ?? '';

    if (originalId.startsWith('park_') && title) {
      const parkId: string = originalId.substring(5);
      return parkId ? ['/', this.currentLang, 'park', parkId, buildParkSlug(title)] : null;
    }

    if (originalId.startsWith('parkItem_') && title && currentItem.parentParkId && currentItem.parentParkName) {
      const itemId: string = originalId.substring(9);
      return itemId
        ? ['/', this.currentLang, 'park', currentItem.parentParkId, buildParkSlug(currentItem.parentParkName), 'item', itemId, buildParkSlug(title)]
        : null;
    }

    return null;
  }

  get hasLogoImage(): boolean {
    return !!this.item?.logoImageId?.trim();
  }

  get hasLocationMeta(): boolean {
    return !!this.item?.city || !!this.item?.countryCode;
  }

  get hasAttractionCount(): boolean {
    return this.isParkResult && this.item?.attractionCount !== null && this.item?.attractionCount !== undefined;
  }

  get isParkResult(): boolean {
    return this.normalizedCategory === 'park' || this.normalizedResourceType === 'parks';
  }

  get displaySubtitle(): string | null {
    const subtitle: string = this.item?.subtitle?.trim() ?? '';
    if (subtitle.length > 0) {
      return subtitle;
    }

    const parentParkName: string = this.item?.parentParkName?.trim() ?? '';
    return parentParkName.length > 0 ? parentParkName : null;
  }

  formatCountryName(countryCode: string | null | undefined): string | null {
    return this.countryDisplayService.resolveLocalizedCountryName(countryCode, this.currentLang);
  }

  formatCount(value: number | null | undefined): string {
    if (value === null || value === undefined) {
      return '0';
    }

    return new Intl.NumberFormat(this.currentLang).format(value);
  }

  private get normalizedCategory(): string {
    return this.normalizeCategory(this.item?.category);
  }

  private get normalizedResourceType(): string {
    return this.normalizeCategory(this.item?.resourceType);
  }

  private normalizeCategory(value: string | null | undefined): string {
    return (value ?? '')
      .trim()
      .toLowerCase()
      .replace(/\s+/g, '')
      .replace(/s$/, '');
  }
}
