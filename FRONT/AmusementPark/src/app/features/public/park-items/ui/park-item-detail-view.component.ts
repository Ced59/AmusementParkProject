import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { LeafletMapComponent } from '@app/components/shared/leaflet-map/leaflet-map.component';
import { ImageDisplayComponent } from '@app/components/shared/image-display/image-display.component';
import { PageStateComponent } from '@app/components/shared/page-state/page-state.component';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { ParkItemDetailViewModel, ParkItemPhotoCategoryOptionViewModel, ParkItemPhotoViewModel } from '../models/park-item-detail-view.model';
import { UiItemCardComponent } from '@ui/cards';
import { UiMapShellComponent, UiMapSlotComponent } from '@ui/maps';
import { UiButtonDirective, UiChipComponent, UiSectionHeaderComponent, UiSurfaceDirective } from '@ui/primitives';

@Component({
  selector: 'app-park-item-detail-view',
  templateUrl: './park-item-detail-view.component.html',
  styleUrls: ['./park-item-detail-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NgIf,
    NgFor,
    RouterLink,
    PageStateComponent,
    ImageDisplayComponent,
    TranslateModule,
    LeafletMapComponent,
    UiButtonDirective,
    UiChipComponent,
    UiItemCardComponent,
    UiMapShellComponent,
    UiMapSlotComponent,
    UiSectionHeaderComponent,
    UiSurfaceDirective
  ]
})
export class ParkItemDetailViewComponent {
  readonly photoDisplayLimits: number[] = [4, 8, 12, 0];

  @Input({ required: true }) state!: Signal<ScreenState<unknown, string>>;
  @Input({ required: true }) detail!: Signal<ParkItemDetailViewModel | null>;

  @Output() backToItemsClicked: EventEmitter<void> = new EventEmitter<void>();

  selectedPhotoCategoryKey: string | null = null;
  selectedPhotoLimit: number = 4;
  activePhotoIndex: number = 0;

  goBackToItems(): void {
    this.backToItemsClicked.emit();
  }

  selectPhotoCategory(categoryKey: string | null): void {
    this.selectedPhotoCategoryKey = categoryKey;
    this.activePhotoIndex = 0;
  }

  setPhotoLimit(limit: number): void {
    this.selectedPhotoLimit = limit;
    this.activePhotoIndex = 0;
  }

  previousPhoto(photos: ParkItemPhotoViewModel[]): void {
    const displayedPhotos: ParkItemPhotoViewModel[] = this.getDisplayedPhotos(photos);

    if (displayedPhotos.length <= 1) {
      return;
    }

    this.activePhotoIndex = this.activePhotoIndex === 0
      ? displayedPhotos.length - 1
      : this.activePhotoIndex - 1;
  }

  nextPhoto(photos: ParkItemPhotoViewModel[]): void {
    const displayedPhotos: ParkItemPhotoViewModel[] = this.getDisplayedPhotos(photos);

    if (displayedPhotos.length <= 1) {
      return;
    }

    this.activePhotoIndex = this.activePhotoIndex >= displayedPhotos.length - 1
      ? 0
      : this.activePhotoIndex + 1;
  }

  getActivePhoto(photos: ParkItemPhotoViewModel[]): ParkItemPhotoViewModel | null {
    const displayedPhotos: ParkItemPhotoViewModel[] = this.getDisplayedPhotos(photos);

    if (displayedPhotos.length === 0) {
      return null;
    }

    return displayedPhotos[Math.min(this.activePhotoIndex, displayedPhotos.length - 1)] ?? displayedPhotos[0];
  }

  getDisplayedPhotos(photos: ParkItemPhotoViewModel[]): ParkItemPhotoViewModel[] {
    const filteredPhotos: ParkItemPhotoViewModel[] = this.getFilteredPhotos(photos);

    if (this.selectedPhotoLimit <= 0) {
      return filteredPhotos;
    }

    return filteredPhotos.slice(0, this.selectedPhotoLimit);
  }

  getFilteredPhotos(photos: ParkItemPhotoViewModel[]): ParkItemPhotoViewModel[] {
    const selectedCategoryKey: string | null = this.resolveSelectedPhotoCategoryKey(photos);

    if (!selectedCategoryKey) {
      return photos;
    }

    return photos.filter((photo: ParkItemPhotoViewModel) => photo.categoryKey === selectedCategoryKey);
  }

  hasSelectedCategory(categoryKey: string | null, photos: ParkItemPhotoViewModel[]): boolean {
    return this.resolveSelectedPhotoCategoryKey(photos) === categoryKey;
  }

  getDisplayLimitLabelKey(limit: number): string {
    return limit <= 0 ? 'parkItems.photos.controls.all' : 'parkItems.photos.controls.count';
  }

  getCategoryCountLabel(category: ParkItemPhotoCategoryOptionViewModel): string {
    return `${category.count}`;
  }

  private resolveSelectedPhotoCategoryKey(photos: ParkItemPhotoViewModel[]): string | null {
    if (!this.selectedPhotoCategoryKey) {
      return null;
    }

    const hasSelectedCategory: boolean = photos.some((photo: ParkItemPhotoViewModel) => photo.categoryKey === this.selectedPhotoCategoryKey);
    return hasSelectedCategory ? this.selectedPhotoCategoryKey : null;
  }
}
