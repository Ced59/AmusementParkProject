import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { PaginatorState } from '@shared/ui/primitives/paginator';
import { Card } from '@shared/ui/primitives/card';
import { FormsModule } from '@angular/forms';
import { InputText } from '@shared/ui/primitives/inputtext';
import { Select } from '@shared/ui/primitives/select';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { Panel } from '@shared/ui/primitives/panel';
import { ProgressSpinner } from '@shared/ui/primitives/progressspinner';
import { ProgressBar } from '@shared/ui/primitives/progressbar';
import { NgClass, DatePipe } from '@angular/common';
import { Tag } from '@shared/ui/primitives/tag';
import { TranslateModule } from '@ngx-translate/core';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { OwnedImageItem } from '@shared/models/images/owned-image-item.model';
import {
  AdminParkItemPhotoCategoryOption,
  AdminParkItemPhotoUploadPreview,
  AdminParkItemPhotoUploadProgress
} from '@features/admin/park-items/models/admin-park-item-edit.model';

@Component({
    selector: 'app-admin-park-item-photos-tab',
    templateUrl: './admin-park-item-photos-tab.component.html',
    styleUrls: ['./admin-park-item-photos-tab.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [Card, FormsModule, InputText, Select, ButtonDirective, Panel, ProgressSpinner, ProgressBar, NgClass, Tag, DatePipe, TranslateModule, PaginationComponent, ImageDisplayComponent]
})
export class AdminParkItemPhotosTabComponent {
  @Input() isEditMode: boolean = false;
  @Input() currentPhoto: OwnedImageItem | null = null;
  @Input() allowMultiplePhotoUpload: boolean = true;
  @Input() selectedPhotoCount: number = 0;
  @Input() selectedPhotoPreviews: AdminParkItemPhotoUploadPreview[] = [];
  @Input() selectedPhotoAnalysisPending: boolean = false;
  @Input() selectedPhotoMissingGeoLocationCount: number = 0;
  @Input() newPhotoDescription: string = '';
  @Input() remotePhotoSourceUrl: string = '';
  @Input() photoWithWatermark: boolean = true;
  @Input() remotePhotoWithWatermark: boolean = false;
  @Input() selectedPhotoCategorySlug: string = 'park-item-gallery';
  @Input() photoCategoryOptions: AdminParkItemPhotoCategoryOption[] = [];
  @Input() photoCategoryLabelKeyByTagId: Record<string, string> = {};
  @Input() photoUploadProgress: AdminParkItemPhotoUploadProgress | null = null;
  @Input() photosUploading: boolean = false;
  @Input() photosLoading: boolean = false;
  @Input() attractionPhotos: OwnedImageItem[] = [];
  @Input() pagedPhotos: OwnedImageItem[] = [];
  @Input() photosPageSize: number = 8;
  @Input() isSaving: boolean = false;

  @Output() photoFileSelected: EventEmitter<Event> = new EventEmitter<Event>();
  @Output() selectedPhotoRemoved: EventEmitter<string> = new EventEmitter<string>();
  @Output() newPhotoDescriptionChange: EventEmitter<string> = new EventEmitter<string>();
  @Output() remotePhotoSourceUrlChange: EventEmitter<string> = new EventEmitter<string>();
  @Output() photoWithWatermarkChange: EventEmitter<boolean> = new EventEmitter<boolean>();
  @Output() remotePhotoWithWatermarkChange: EventEmitter<boolean> = new EventEmitter<boolean>();
  @Output() selectedPhotoCategorySlugChange: EventEmitter<string> = new EventEmitter<string>();
  @Output() uploadPhoto: EventEmitter<void> = new EventEmitter<void>();
  @Output() importRemotePhoto: EventEmitter<void> = new EventEmitter<void>();
  @Output() setCurrentPhoto: EventEmitter<OwnedImageItem> = new EventEmitter<OwnedImageItem>();
  @Output() deletePhoto: EventEmitter<OwnedImageItem> = new EventEmitter<OwnedImageItem>();
  @Output() photosPageChange: EventEmitter<PaginatorState> = new EventEmitter<PaginatorState>();
  @Output() saveSection: EventEmitter<void> = new EventEmitter<void>();

  get uploadProgressPercentage(): number {
    if (!this.photoUploadProgress || this.photoUploadProgress.total <= 0) {
      return 0;
    }

    return Math.round((this.photoUploadProgress.completed / this.photoUploadProgress.total) * 100);
  }

  get canUploadSelectedPhotos(): boolean {
    return this.selectedPhotoCount > 0 && !this.photosUploading && !this.selectedPhotoAnalysisPending;
  }

  formatFileSize(sizeInBytes: number): string {
    if (sizeInBytes < 1024) {
      return `${sizeInBytes} B`;
    }

    const sizeInKilobytes: number = sizeInBytes / 1024;
    if (sizeInKilobytes < 1024) {
      return `${sizeInKilobytes.toFixed(1)} KB`;
    }

    return `${(sizeInKilobytes / 1024).toFixed(1)} MB`;
  }

  resolvePhotoCategoryLabelKey(photo: OwnedImageItem): string | null {
    for (const tagId of photo.tagIds) {
      const labelKey: string | undefined = this.photoCategoryLabelKeyByTagId[tagId];
      if (labelKey) {
        return labelKey;
      }
    }

    return null;
  }
}
