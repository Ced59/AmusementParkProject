import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { DatePipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { PaginatorState } from '@shared/ui/primitives/paginator';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { Card } from '@shared/ui/primitives/card';
import { InputText } from '@shared/ui/primitives/inputtext';
import { Panel } from '@shared/ui/primitives/panel';
import { ProgressSpinner } from '@shared/ui/primitives/progressspinner';
import { Select } from '@shared/ui/primitives/select';
import { Tag } from '@shared/ui/primitives/tag';

import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { AdminParkPhotoCategoryOption } from '@features/admin/parks/models/admin-park-edit.model';
import { OwnedImageItem } from '@shared/models/images/owned-image-item.model';

@Component({
  selector: 'app-admin-park-photos-tab',
  templateUrl: './admin-park-photos-tab.component.html',
  styleUrls: ['./admin-park-photos-tab.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Card,
    FormsModule,
    InputText,
    Select,
    ButtonDirective,
    Panel,
    ProgressSpinner,
    NgClass,
    Tag,
    DatePipe,
    TranslateModule,
    PaginationComponent,
    ImageDisplayComponent
  ]
})
export class AdminParkPhotosTabComponent {
  @Input() isEditMode: boolean = false;
  @Input() currentPhoto: OwnedImageItem | null = null;
  @Input() allowMultiplePhotoUpload: boolean = true;
  @Input() selectedPhotoCount: number = 0;
  @Input() newPhotoDescription: string = '';
  @Input() remotePhotoSourceUrl: string = '';
  @Input() photoWithWatermark: boolean = true;
  @Input() remotePhotoWithWatermark: boolean = false;
  @Input() selectedPhotoCategorySlug: string = 'park-gallery';
  @Input() photoCategoryOptions: AdminParkPhotoCategoryOption[] = [];
  @Input() photosUploading: boolean = false;
  @Input() photosLoading: boolean = false;
  @Input() parkPhotos: OwnedImageItem[] = [];
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
