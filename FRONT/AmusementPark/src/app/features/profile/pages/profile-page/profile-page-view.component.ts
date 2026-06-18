import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { UserDto } from '@app/models/users/user_dto';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageCategory } from '@app/models/images/image-category';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { MeasurementSystem } from '@shared/models/measurements/measurement-system.model';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { OwnerImageUploadDialogComponent } from '@shared/components/owner-image-upload-dialog/owner-image-upload-dialog.component';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSectionHeaderComponent, UiSurfaceDirective } from '@ui/primitives';
import { UiFieldInputComponent } from '@ui/forms';

@Component({
  selector: 'app-profile-page-view',
  templateUrl: './profile-page-view.component.html',
  styleUrls: ['./profile-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageStateComponent,
    OwnerImageUploadDialogComponent,
    TranslateModule,
    ImageDisplayComponent,
    UiButtonDirective,
    UiChipComponent,
    UiFieldInputComponent,
    UiKickerComponent,
    UiSectionHeaderComponent,
    UiSurfaceDirective
  ]
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
  @Output() preferredMeasurementSystemChanged: EventEmitter<MeasurementSystem> = new EventEmitter<MeasurementSystem>();
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

  selectPreferredMeasurementSystem(system: MeasurementSystem): void {
    this.preferredMeasurementSystemChanged.emit(system);
  }

  measurementSystemLabelKey(system: string | null | undefined): string {
    return system === 'Imperial' ? 'measurementSystem.imperial' : 'measurementSystem.metric';
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
