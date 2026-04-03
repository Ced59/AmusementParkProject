import { Component, EventEmitter, Input, Output } from '@angular/core';
import { PaginatorState, Paginator } from 'primeng/paginator';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { FormsModule } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { ButtonDirective } from 'primeng/button';
import { Panel } from 'primeng/panel';
import { ProgressSpinner } from 'primeng/progressspinner';
import { NgClass, DatePipe } from '@angular/common';
import { Tag } from 'primeng/tag';
import { TranslateModule } from '@ngx-translate/core';

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
    styleUrls: ['./admin-park-item-photos-tab.component.scss'],
    imports: [Bind, Card, FormsModule, InputText, ButtonDirective, Panel, ProgressSpinner, NgClass, Tag, Paginator, DatePipe, TranslateModule]
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
