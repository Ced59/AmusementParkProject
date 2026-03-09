import { Component, EventEmitter, Input, Output } from '@angular/core';
import { PaginatorState } from 'primeng/paginator';

interface AttractionPhotoItem {
  id: string;
  imageUrl: string;
  description?: string;
  isCurrent: boolean;
  createdAt: string;
}

@Component({
  selector: 'app-admin-park-item-photos-tab',
  templateUrl: './admin-park-item-photos-tab.component.html',
  styleUrls: ['./admin-park-item-photos-tab.component.scss']
})
export class AdminParkItemPhotosTabComponent {
  @Input() isEditMode: boolean = false;
  @Input() currentPhoto: AttractionPhotoItem | null = null;
  @Input() allowMultiplePhotoUpload: boolean = true;
  @Input() selectedPhotoCount: number = 0;
  @Input() newPhotoDescription: string = '';
  @Input() photosUploading: boolean = false;
  @Input() photosLoading: boolean = false;
  @Input() attractionPhotos: AttractionPhotoItem[] = [];
  @Input() pagedPhotos: AttractionPhotoItem[] = [];
  @Input() photosPageSize: number = 8;
  @Input() isSaving: boolean = false;

  @Output() photoFileSelected: EventEmitter<Event> = new EventEmitter<Event>();
  @Output() newPhotoDescriptionChange: EventEmitter<string> = new EventEmitter<string>();
  @Output() uploadPhoto: EventEmitter<void> = new EventEmitter<void>();
  @Output() setCurrentPhoto: EventEmitter<AttractionPhotoItem> = new EventEmitter<AttractionPhotoItem>();
  @Output() deletePhoto: EventEmitter<AttractionPhotoItem> = new EventEmitter<AttractionPhotoItem>();
  @Output() photosPageChange: EventEmitter<PaginatorState> = new EventEmitter<PaginatorState>();
  @Output() saveSection: EventEmitter<void> = new EventEmitter<void>();
}
