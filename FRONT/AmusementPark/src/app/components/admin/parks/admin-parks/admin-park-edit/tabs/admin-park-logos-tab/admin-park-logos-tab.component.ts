import { Component, EventEmitter, Input, Output } from '@angular/core';
import { PaginatorState } from 'primeng/paginator';

interface ParkLogoItem {
  id: string;
  imageUrl: string;
  description?: string;
  isCurrent: boolean;
  createdAt: string;
}

@Component({
  selector: 'app-admin-park-logos-tab',
  templateUrl: './admin-park-logos-tab.component.html',
  styleUrls: ['./admin-park-logos-tab.component.scss']
})
export class AdminParkLogosTabComponent {
  @Input() isEditMode: boolean = false;
  @Input() currentLogo: ParkLogoItem | null = null;
  @Input() allowMultipleLogoUpload: boolean = true;
  @Input() selectedLogoCount: number = 0;
  @Input() newLogoDescription: string = '';
  @Input() logosUploading: boolean = false;
  @Input() logosLoading: boolean = false;
  @Input() parkLogos: ParkLogoItem[] = [];
  @Input() pagedLogos: ParkLogoItem[] = [];
  @Input() logosPageSize: number = 8;
  @Input() isSaving: boolean = false;

  @Output() logoFileSelected: EventEmitter<Event> = new EventEmitter<Event>();
  @Output() newLogoDescriptionChange: EventEmitter<string> = new EventEmitter<string>();
  @Output() uploadLogo: EventEmitter<void> = new EventEmitter<void>();
  @Output() setCurrentLogo: EventEmitter<ParkLogoItem> = new EventEmitter<ParkLogoItem>();
  @Output() deleteLogo: EventEmitter<ParkLogoItem> = new EventEmitter<ParkLogoItem>();
  @Output() logosPageChange: EventEmitter<PaginatorState> = new EventEmitter<PaginatorState>();
  @Output() saveSection: EventEmitter<void> = new EventEmitter<void>();
}
