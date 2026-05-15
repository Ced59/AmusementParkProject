import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject } from '@angular/core';
import { distinctUntilChanged } from 'rxjs';
import { Router } from '@angular/router';
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
import { ProfilePageViewComponent } from './profile-page-view.component';
import { ProfilePageStateFacade } from '@features/profile/state/profile-page-state.facade';

@Component({
    selector: 'app-profile-page',
    templateUrl: './profile-page.component.html',
    styleUrl: './profile-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ProfilePageStateFacade],
    imports: [ProfilePageViewComponent]
})
export class ProfilePageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly user = this.stateFacade.user;
  displayAvatarUploadDialog: boolean = false;
  isEditingIdentity: boolean = false;
  savingIdentity: boolean = false;
  identityDraft = {
    firstName: '',
    lastName: ''
  };

  protected readonly avatarCategory = ImageCategory.AVATAR;
  protected readonly userOwnerType = ImageOwnerType.USER;
  protected currentUserId: string | null = null;

  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  constructor(
    private readonly stateFacade: ProfilePageStateFacade,
    private readonly usersApiService: UsersApiService,
    private readonly imagesApiService: ImagesApiService,
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly sharedService: SharedService,
    private readonly modalService: ModalService,
    private readonly translationService: TranslationService,
    private readonly messageService: ToastMessageService
  ) {
  }

  ngOnInit(): void {
    this.currentUserId = this.authService.getUserIdFromToken();

    if (this.currentUserId) {
      this.stateFacade.loadUserProfile(this.currentUserId);
    } else {
      this.stateFacade.setError();
    }

    this.translationService.languageChanged.pipe(
      distinctUntilChanged(),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((lang: string) => {
      this.updatePreferredLanguage(lang);
    });
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
      lastName: currentUser.lastName ?? ''
    };
    this.isEditingIdentity = true;
  }

  cancelIdentityEdition(): void {
    const currentUser: UserDto | null = this.user();

    this.isEditingIdentity = false;
    this.savingIdentity = false;
    this.identityDraft = {
      firstName: currentUser?.firstName ?? '',
      lastName: currentUser?.lastName ?? ''
    };
  }

  saveIdentity(): void {
    const currentUser: UserDto | null = this.user();

    if (!this.currentUserId || !currentUser) {
      return;
    }

    const firstName: string = this.identityDraft.firstName.trim();
    const lastName: string = this.identityDraft.lastName.trim();

    if (!firstName || !lastName) {
      this.messageService.add('warn', 'Attention', 'Le prénom et le nom sont requis.');
      return;
    }

    this.savingIdentity = true;

    const payload: UserPut = {
      firstName,
      lastName,
      email: currentUser.email ?? '',
      newEmail: currentUser.email ?? '',
      preferredLanguage: currentUser.preferredLanguage ?? this.translationService.getCurrentLang().toUpperCase()
    };

    this.usersApiService.putUserById(this.currentUserId, payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (user: UserDto) => {
        this.stateFacade.setUser(user);
        this.identityDraft = {
          firstName: user.firstName ?? '',
          lastName: user.lastName ?? ''
        };
        this.isEditingIdentity = false;
        this.savingIdentity = false;
        this.sharedService.emitLoginStatusChange();
        this.messageService.add('success', 'Succès', 'Profil mis à jour avec succès !');
      },
      error: (error: unknown) => {
        console.error('Error updating profile identity', error);
        this.savingIdentity = false;
        this.messageService.add('error', 'Erreur', 'La mise à jour du profil a échoué.');
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
    this.messageService.add('success', 'Succès', 'Avatar mis à jour avec succès !');
  }

  getAvatarUrl(): string {
    return this.imagesApiService.resolveImageUrl(this.user()?.avatarUrl) ?? '';
  }

  logout(): void {
    this.authService.logout();
    this.sharedService.emitLoginStatusChange();
    const currentLang: string = this.router.url.split('/')[1] || 'en';
    this.router.navigate([currentLang, 'home']);
  }

  private updatePreferredLanguage(lang: string): void {
    const currentUser: UserDto | null = this.user();

    if (!this.currentUserId || !currentUser) {
      return;
    }

    const payload: UserPut = {
      firstName: currentUser.firstName ?? '',
      lastName: currentUser.lastName ?? '',
      email: currentUser.email ?? '',
      newEmail: currentUser.email ?? '',
      preferredLanguage: lang.toUpperCase()
    };

    this.usersApiService.putUserById(this.currentUserId, payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (user: UserDto) => {
        this.stateFacade.setUser(user);
      }
    });
  }
}
