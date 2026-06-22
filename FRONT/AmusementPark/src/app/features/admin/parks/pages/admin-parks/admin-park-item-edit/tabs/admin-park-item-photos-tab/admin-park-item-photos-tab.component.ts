import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { PaginatorState } from 'primeng/paginator';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { FormsModule } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { ButtonDirective } from 'primeng/button';
import { Panel } from 'primeng/panel';
import { ProgressSpinner } from 'primeng/progressspinner';
import { NgClass, DatePipe } from '@angular/common';
import { Tag } from 'primeng/tag';
import { TranslateModule } from '@ngx-translate/core';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { OwnedImageItem } from '@shared/models/images/owned-image-item.model';
import { AdminParkItemPhotoCategoryOption } from '@features/admin/park-items/models/admin-park-item-edit.model';

@Component({
    selector: 'app-admin-park-item-photos-tab',
    templateUrl: './admin-park-item-photos-tab.component.html',
    styleUrls: ['./admin-park-item-photos-tab.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [Bind, Card, FormsModule, InputText, Select, ButtonDirective, Panel, ProgressSpinner, NgClass, Tag, DatePipe, TranslateModule, PaginationComponent, ImageDisplayComponent]
})
export class AdminParkItemPhotosTabComponent {
  @Input() isEditMode: boolean = false;
  @Input() currentPhoto: OwnedImageItem | null = null;
  @Input() allowMultiplePhotoUpload: boolean = true;
  @Input() selectedPhotoCount: number = 0;
  @Input() newPhotoDescription: string = '';
  @Input() remotePhotoSourceUrl: string = '';
  @Input() photoWithWatermark: boolean = true;
  @Input() remotePhotoWithWatermark: boolean = false;
  @Input() selectedPhotoCategorySlug: string = 'park-item-gallery';
  @Input() photoCategoryOptions: AdminParkItemPhotoCategoryOption[] = [];
  @Input() photosUploading: boolean = false;
  @Input() photosLoading: boolean = false;
  @Input() attractionPhotos: OwnedImageItem[] = [];
  @Input() pagedPhotos: OwnedImageItem[] = [];
  @Input() photosPageSize: number = 8;
  @Input() isSaving: boolean = false;

  @Output() photoFileSelected: EventEmitter<Event> = new EventEmitter<Event>();
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
}
