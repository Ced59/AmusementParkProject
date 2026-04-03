import { Component, EventEmitter, Input, Output } from '@angular/core';
import { PaginatorState, Paginator } from 'primeng/paginator';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { ImageDisplayComponent } from '../../../../../../shared/image-display/image-display.component';
import { FormsModule } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { ButtonDirective } from 'primeng/button';
import { Panel } from 'primeng/panel';
import { ProgressSpinner } from 'primeng/progressspinner';
import { NgClass, DatePipe } from '@angular/common';
import { Tag } from 'primeng/tag';
import { TranslateModule } from '@ngx-translate/core';

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
    styleUrls: ['./admin-park-logos-tab.component.scss'],
    imports: [Bind, Card, ImageDisplayComponent, FormsModule, InputText, ButtonDirective, Panel, ProgressSpinner, NgClass, Tag, Paginator, DatePipe, TranslateModule]
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
