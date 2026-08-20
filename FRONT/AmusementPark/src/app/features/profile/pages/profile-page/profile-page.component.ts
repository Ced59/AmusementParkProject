import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { UserDto } from '@app/models/users/user_dto';
import { UserPut } from '@app/models/users/user_put';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageCategory } from '@app/models/images/image-category';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { UsersApiService } from '@data-access/users/users-api.service';
import { AuthService } from '@app/services/auth/auth.service';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { ModalService } from '@app/services/modal/modal.service';
import { SharedService } from '@app/services/shared/shared.service';
import { TranslationService } from '@app/services/translation.service';
import { MeasurementPreferenceService } from '@app/services/measurements/measurement-preference.service';
import { MeasurementSystem } from '@shared/models/measurements/measurement-system.model';
import { TranslateService } from '@ngx-translate/core';
import { ProfilePageViewComponent } from './profile-page-view.component';
import { ProfilePageStateFacade } from '@features/profile/state/profile-page-state.facade';
import { extractApiProblemDetails } from '@shared/utils/security/error-display.helpers';
import { LanguagePreferenceService } from '@app/services/localization/language-preference.service';

@Component({
    selector: 'app-profile-page',
    templateUrl: './profile-page.component.html',
    styleUrl: './profile-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ProfilePageStateFacade],
    imports: [ProfilePageViewComponent]
})
export class ProfilePageComponent implements OnInit {
  private static readonly MaximumPublicDisplayNameLength = 60;

  protected readonly state = this.stateFacade.state;
  protected readonly user = this.stateFacade.user;
  displayAvatarUploadDialog: boolean = false;
  isEditingIdentity: boolean = false;
  savingIdentity: boolean = false;
  identityDraft = {
    firstName: '',
    lastName: '',
    publicDisplayName: ''
  };

  protected readonly avatarCategory = ImageCategory.AVATAR;
  protected readonly userOwnerType = ImageOwnerType.USER;
  protected currentUserId: string | null = null;
  protected initialTab: 'profile' | 'ratings' = 'profile';

  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  constructor(
    private readonly stateFacade: ProfilePageStateFacade,
    private readonly activatedRoute: ActivatedRoute,
    private readonly usersApiService: UsersApiService,
    private readonly imagesApiService: ImagesApiService,
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly sharedService: SharedService,
    private readonly modalService: ModalService,
    private readonly translationService: TranslationService,
    private readonly languagePreferenceService: LanguagePreferenceService,
    private readonly measurementPreferenceService: MeasurementPreferenceService,
    private readonly translateService: TranslateService,
    private readonly messageService: ToastMessageService
  ) {
    effect((): void => {
      this.measurementPreferenceService.syncFromUser(this.user());
    });

    effect((): void => {
      const currentUser: UserDto | null = this.user();
      const preferredLanguage: string | null = this.languagePreferenceService.preferredLanguage();
      if (!currentUser || preferredLanguage === null || currentUser.preferredLanguage.toLowerCase() === preferredLanguage) {
        return;
      }

      this.stateFacade.setUser({
        ...currentUser,
        preferredLanguage: preferredLanguage.toUpperCase()
      });
    });
  }

  ngOnInit(): void {
    this.initialTab = this.activatedRoute.snapshot.queryParamMap.get('tab') === 'ratings'
      ? 'ratings'
      : 'profile';
    this.currentUserId = this.authService.getUserIdFromToken();

    if (this.currentUserId) {
      this.stateFacade.loadUserProfile(this.currentUserId);
    } else {
      this.stateFacade.setError();
    }

  }

  editField(field: string): void {
    if (field === 'avatar') {
      this.displayAvatarUploadDialog = true;
      return;
    }

    if (field === 'identity') {
      this.startIdentityEdition();
    }
  }

  editPreferredLanguage(): void {
    this.modalService.openModal('languageModal');
  }

  startIdentityEdition(): void {
    const currentUser: UserDto | null = this.user();

    if (!currentUser) {
      return;
    }

    this.identityDraft = {
      firstName: currentUser.firstName ?? '',
      lastName: currentUser.lastName ?? '',
      publicDisplayName: currentUser.publicDisplayName ?? ''
    };
    this.isEditingIdentity = true;
  }

  cancelIdentityEdition(): void {
    const currentUser: UserDto | null = this.user();

    this.isEditingIdentity = false;
    this.savingIdentity = false;
    this.identityDraft = {
      firstName: currentUser?.firstName ?? '',
      lastName: currentUser?.lastName ?? '',
      publicDisplayName: currentUser?.publicDisplayName ?? ''
    };
  }

  saveIdentity(): void {
    const currentUser: UserDto | null = this.user();

    if (!this.currentUserId || !currentUser) {
      return;
    }

    const firstName: string = this.identityDraft.firstName.trim();
    const lastName: string = this.identityDraft.lastName.trim();
    const publicDisplayName: string = this.identityDraft.publicDisplayName.trim();

    if (!firstName || !lastName) {
      this.messageService.add('warn', this.translate('common.warning', 'Warning'), this.translate('user-profile.identityRequired', 'First name and last name are required.'));
      return;
    }

    if (publicDisplayName.length > ProfilePageComponent.MaximumPublicDisplayNameLength) {
      this.messageService.add(
        'warn',
        this.translate('common.warning', 'Warning'),
        this.translate('user-profile.publicDisplayNameTooLong', 'The public name is too long.')
      );
      return;
    }

    this.savingIdentity = true;

    const payload: UserPut = {
      firstName,
      lastName,
      publicDisplayName,
      email: currentUser.email ?? '',
      newEmail: currentUser.email ?? '',
      preferredLanguage: currentUser.preferredLanguage ?? this.translationService.getCurrentLang().toUpperCase(),
      preferredMeasurementSystem: currentUser.preferredMeasurementSystem ?? this.measurementPreferenceService.getPreferredSystem()
    };

    this.usersApiService.putUserById(this.currentUserId, payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (user: UserDto) => {
        this.stateFacade.setUser(user);
        this.identityDraft = {
          firstName: user.firstName ?? '',
          lastName: user.lastName ?? '',
          publicDisplayName: user.publicDisplayName ?? ''
        };
        this.isEditingIdentity = false;
        this.savingIdentity = false;
        this.sharedService.emitLoginStatusChange();
        this.messageService.add('success', this.translate('common.success', 'Success'), this.translate('user-profile.updateSuccess', 'Profile updated successfully.'));
      },
      error: (error: unknown) => {
        console.error('Error updating profile identity', error);
        this.savingIdentity = false;
        this.messageService.add(
          'error',
          this.translate('common.error', 'Error'),
          this.translate(this.resolvePublicDisplayNameErrorKey(error), 'Profile update failed.')
        );
      }
    });
  }

  onAvatarUploadDialogVisibleChange(visible: boolean): void {
    this.displayAvatarUploadDialog = visible;
  }

  onAvatarUploaded(image: ImageDto): void {
    void image;

    if (!this.currentUserId) {
      return;
    }

    this.stateFacade.loadUserProfile(this.currentUserId);
    this.sharedService.emitLoginStatusChange();
    this.messageService.add('success', this.translate('common.success', 'Success'), this.translate('user-profile.avatar.updateSuccess', 'Avatar updated successfully.'));
  }

  getAvatarUrl(): string {
    return this.imagesApiService.resolveImageUrl(this.user()?.avatarUrl) ?? '';
  }

  logout(): void {
    this.authService.logout();
    this.sharedService.emitLoginStatusChange();
    const currentLang: string = this.router.url.split('/')[1] || 'en';
    this.router.navigate(['/', currentLang, 'home']);
  }

  updatePreferredMeasurementSystem(system: MeasurementSystem): void {
    const currentUser: UserDto | null = this.user();

    if (!this.currentUserId || !currentUser) {
      this.measurementPreferenceService.setPreferredSystem(system);
      return;
    }

    const payload: UserPut = {
      firstName: currentUser.firstName ?? '',
      lastName: currentUser.lastName ?? '',
      publicDisplayName: currentUser.publicDisplayName ?? '',
      email: currentUser.email ?? '',
      newEmail: currentUser.email ?? '',
      preferredLanguage: currentUser.preferredLanguage ?? this.translationService.getCurrentLang().toUpperCase(),
      preferredMeasurementSystem: system
    };

    this.usersApiService.putUserById(this.currentUserId, payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (user: UserDto) => {
        this.stateFacade.setUser(user);
        this.measurementPreferenceService.syncFromUser(user);
        this.messageService.add('success', this.translate('common.success', 'Success'), this.translate('user-profile.updateSuccess', 'Profile updated successfully.'));
      },
      error: (error: unknown) => {
        console.error('Error updating preferred measurement system', error);
        this.messageService.add('error', this.translate('common.error', 'Error'), this.translate('user-profile.updateError', 'Profile update failed.'));
      }
    });
  }

  private translate(key: string, fallback: string): string {
    const translatedValue: string = this.translateService.instant(key);
    return translatedValue === key ? fallback : translatedValue;
  }

  private resolvePublicDisplayNameErrorKey(error: unknown): string {
    const errorCode: string | null | undefined = extractApiProblemDetails(error)?.errorCode;
    if (errorCode === 'user.public-display-name.already-exists') {
      return 'user-profile.publicDisplayNameTaken';
    }

    if (errorCode === 'user.public-display-name.reserved') {
      return 'user-profile.publicDisplayNameReserved';
    }

    if (errorCode === 'user.public-display-name.invalid') {
      return 'user-profile.publicDisplayNameTooLong';
    }

    return 'user-profile.updateError';
  }
}
