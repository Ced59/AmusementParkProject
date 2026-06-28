import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { AdminParkItemPhotoCategoryOption } from '@features/admin/park-items/models/admin-park-item-edit.model';
import { AdminParkPhotoCategoryOption } from '@features/admin/parks/models/admin-park-edit.model';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';

import {
  AdminPhotoBatchOwnerKind,
  AdminPhotoBatchParkItemOption,
  AdminPhotoBatchPhoto
} from '../../../models/admin-photo-batch.model';

@Component({
  selector: 'app-admin-photo-batch-card',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, ImageDisplayComponent],
  templateUrl: './admin-photo-batch-card.component.html',
  styleUrl: './admin-photo-batch-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminPhotoBatchCardComponent {
  @Input({ required: true }) photo!: AdminPhotoBatchPhoto;
  @Input() parkItems: readonly AdminPhotoBatchParkItemOption[] = [];
  @Input() parkCategoryOptions: readonly AdminParkPhotoCategoryOption[] = [];
  @Input() parkItemCategoryOptions: readonly AdminParkItemPhotoCategoryOption[] = [];

  @Output() ownerKindChange = new EventEmitter<{ photoId: string; ownerKind: AdminPhotoBatchOwnerKind }>();
  @Output() parkItemChange = new EventEmitter<{ photoId: string; parkItemId: string }>();
  @Output() categoryChange = new EventEmitter<{ photoId: string; categorySlug: string }>();
  @Output() save = new EventEmitter<string>();
  @Output() uncategorize = new EventEmitter<string>();
  @Output() togglePublished = new EventEmitter<string>();
  @Output() deletePhoto = new EventEmitter<AdminPhotoBatchPhoto>();

  protected get categoryOptions(): readonly (AdminParkPhotoCategoryOption | AdminParkItemPhotoCategoryOption)[] {
    return this.photo.draftOwnerKind === 'parkItem'
      ? this.parkItemCategoryOptions
      : this.parkCategoryOptions;
  }

  protected get canSave(): boolean {
    return !this.photo.isSaving &&
      (this.photo.draftOwnerKind === 'park' || Boolean(this.photo.draftParkItemId)) &&
      Boolean(this.photo.draftCategorySlug);
  }

  protected get hasGeoLocation(): boolean {
    return Number.isFinite(this.photo.image.geoLocation?.latitude) && Number.isFinite(this.photo.image.geoLocation?.longitude);
  }

  protected onOwnerKindChange(ownerKind: AdminPhotoBatchOwnerKind): void {
    this.ownerKindChange.emit({ photoId: this.photo.id, ownerKind });
  }

  protected onParkItemChange(parkItemId: string): void {
    this.parkItemChange.emit({ photoId: this.photo.id, parkItemId });
  }

  protected onCategoryChange(categorySlug: string): void {
    this.categoryChange.emit({ photoId: this.photo.id, categorySlug });
  }

  protected savePhoto(): void {
    if (this.canSave) {
      this.save.emit(this.photo.id);
    }
  }

  protected moveToUncategorized(): void {
    if (!this.photo.isSaving) {
      this.uncategorize.emit(this.photo.id);
    }
  }

  protected togglePublicVisibility(): void {
    if (!this.photo.isSaving) {
      this.togglePublished.emit(this.photo.id);
    }
  }

  protected deleteCurrentPhoto(): void {
    if (!this.photo.isSaving) {
      this.deletePhoto.emit(this.photo);
    }
  }
}
