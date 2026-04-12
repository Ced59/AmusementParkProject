import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { ButtonDirective } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { TranslateModule } from '@ngx-translate/core';
import { UserDto } from '../../../../models/users/user_dto';
import { ImageDto } from '../../../../models/images/image-dto';
import { ImageCategory } from '../../../../models/images/image-category';
import { ImageOwnerType } from '../../../../models/images/image-owner-type';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { PageStateComponent } from '../../../shared/page-state/page-state.component';
import { OwnerImageUploadDialogComponent } from '../../../shared/owner-image-upload-dialog/owner-image-upload-dialog.component';

@Component({
  selector: 'app-profile-page-view',
  templateUrl: './profile-page-view.component.html',
  styleUrls: ['./profile-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageStateComponent, Bind, Card, ButtonDirective, NgIf, FormsModule, InputText, OwnerImageUploadDialogComponent, TranslateModule]
})
export class ProfilePageViewComponent {
  @Input() state!: Signal<ScreenState<unknown, string>>;
  @Input() user!: Signal<UserDto | null>;
  @Input() displayAvatarUploadDialog: boolean = false;
  @Input() isEditingIdentity: boolean = false;
  @Input() savingIdentity: boolean = false;
  @Input() identityDraft: { firstName: string; lastName: string } = { firstName: '', lastName: '' };
  @Input() avatarCategory!: ImageCategory;
  @Input() userOwnerType!: ImageOwnerType;
  @Input() currentUserId: string | null = null;
  @Input() avatarUrl: string = '';

  @Output() editFieldClicked: EventEmitter<string> = new EventEmitter<string>();
  @Output() preferredLanguageEditClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() saveIdentityClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() cancelIdentityClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() logoutClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() avatarDialogVisibleChange: EventEmitter<boolean> = new EventEmitter<boolean>();
  @Output() avatarUploaded: EventEmitter<ImageDto> = new EventEmitter<ImageDto>();

  editField(field: string): void {
    this.editFieldClicked.emit(field);
  }

  editPreferredLanguage(): void {
    this.preferredLanguageEditClicked.emit();
  }

  saveIdentity(): void {
    this.saveIdentityClicked.emit();
  }

  cancelIdentityEdition(): void {
    this.cancelIdentityClicked.emit();
  }

  logout(): void {
    this.logoutClicked.emit();
  }

  onAvatarUploadDialogVisibleChange(visible: boolean): void {
    this.avatarDialogVisibleChange.emit(visible);
  }

  onAvatarUploaded(image: ImageDto): void {
    this.avatarUploaded.emit(image);
  }
}
