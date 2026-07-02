import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { PaginatorState } from '@shared/primeless/paginator';
import { Card } from '@shared/primeless/card';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { FormsModule } from '@angular/forms';
import { InputText } from '@shared/primeless/inputtext';
import { ButtonDirective } from '@shared/primeless/button';
import { Panel } from '@shared/primeless/panel';
import { ProgressSpinner } from '@shared/primeless/progressspinner';
import { NgClass, DatePipe } from '@angular/common';
import { Tag } from '@shared/primeless/tag';
import { TranslateModule } from '@ngx-translate/core';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { OwnedImageItem } from '@shared/models/images/owned-image-item.model';

@Component({
    selector: 'app-admin-park-logos-tab',
    templateUrl: './admin-park-logos-tab.component.html',
    styleUrls: ['./admin-park-logos-tab.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [Card, ImageDisplayComponent, FormsModule, InputText, ButtonDirective, Panel, ProgressSpinner, NgClass, Tag, DatePipe, TranslateModule, PaginationComponent]
})
export class AdminParkLogosTabComponent {
  @Input() isEditMode: boolean = false;
  @Input() currentLogo: OwnedImageItem | null = null;
  @Input() allowMultipleLogoUpload: boolean = true;
  @Input() selectedLogoCount: number = 0;
  @Input() newLogoDescription: string = '';
  @Input() remoteLogoSourceUrl: string = '';
  @Input() logosUploading: boolean = false;
  @Input() logosLoading: boolean = false;
  @Input() parkLogos: OwnedImageItem[] = [];
  @Input() pagedLogos: OwnedImageItem[] = [];
  @Input() logosPageSize: number = 8;
  @Input() isSaving: boolean = false;

  @Output() logoFileSelected: EventEmitter<Event> = new EventEmitter<Event>();
  @Output() newLogoDescriptionChange: EventEmitter<string> = new EventEmitter<string>();
  @Output() remoteLogoSourceUrlChange: EventEmitter<string> = new EventEmitter<string>();
  @Output() uploadLogo: EventEmitter<void> = new EventEmitter<void>();
  @Output() importRemoteLogo: EventEmitter<void> = new EventEmitter<void>();
  @Output() setCurrentLogo: EventEmitter<OwnedImageItem> = new EventEmitter<OwnedImageItem>();
  @Output() deleteLogo: EventEmitter<OwnedImageItem> = new EventEmitter<OwnedImageItem>();
  @Output() logosPageChange: EventEmitter<PaginatorState> = new EventEmitter<PaginatorState>();
  @Output() saveSection: EventEmitter<void> = new EventEmitter<void>();
}
