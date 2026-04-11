import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { distinctUntilChanged, Subscription } from 'rxjs';
import { Router } from '@angular/router';

import { UserDto } from '../../../../models/users/user_dto';
import { UserPut } from '../../../../models/users/user_put';
import { ImageDto } from '../../../../models/images/image-dto';
import { ImageCategory } from '../../../../models/images/image-category';
import { ImageOwnerType } from '../../../../models/images/image-owner-type';
import { ViewState } from '../../../../models/shared/view-state';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { UsersApiService } from '@data-access/users/users-api.service';
import { AuthService } from '../../../../services/auth/auth.service';
import { ToastMessageService } from '../../../../services/messages/toast-message.service';
import { ModalService } from '../../../../services/modal/modal.service';
import { SharedService } from '../../../../services/shared/shared.service';
import { TranslationService } from '../../../../services/translation.service';
import { commitViewUpdate } from '../../../../utils/change-detection.utils';
import { PageStateComponent } from '../../../shared/page-state/page-state.component';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { ButtonDirective } from 'primeng/button';
import { NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { OwnerImageUploadDialogComponent } from '../../../shared/owner-image-upload-dialog/owner-image-upload-dialog.component';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-profile-page',
    templateUrl: './profile-page.component.html',
    styleUrl: './profile-page.component.scss',
    imports: [PageStateComponent, Bind, Card, ButtonDirective, NgIf, FormsModule, InputText, OwnerImageUploadDialogComponent, TranslateModule]
})
export class ProfilePageComponent implements OnInit, OnDestroy {
  user: UserDto | null = null;
  userPut: UserPut | null = null;
  pageState: ViewState = ViewState.Loading;
  displayAvatarUploadDialog: boolean = false;
  isEditingIdentity: boolean = false;
  savingIdentity: boolean = false;
  identityDraft = {
    firstName: '',
    lastName: ''
  };

  protected readonly avatarCategory = ImageCategory.AVATAR;
  protected readonly userOwnerType = ImageOwnerType.USER;
  protected readonly viewState = ViewState;

  private readonly subscriptions: Subscription = new Subscription();
  protected currentUserId: string | null = null;

  constructor(
    private readonly usersApiService: UsersApiService,
    private readonly imagesApiService: ImagesApiService,
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly sharedService: SharedService,
    private readonly modalService: ModalService,
    private readonly translationService: TranslationService,
    private readonly messageService: ToastMessageService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
    void this.modalService;
  }

  ngOnInit(): void {
    this.currentUserId = this.authService.getUserIdFromToken();

    if (this.currentUserId) {
      this.loadUserProfile(this.currentUserId);
    } else {
      commitViewUpdate(this.changeDetectorRef, () => {
        this.pageState = ViewState.Error;
      });
    }

    this.subscriptions.add(
      this.translationService.languageChanged
        .pipe(distinctUntilChanged())
        .subscribe((lang: string) => {
          this.updatePreferredLanguage(lang);
        })
    );
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
    if (!this.user) {
      return;
    }

    this.identityDraft = {
      firstName: this.user.firstName ?? '',
      lastName: this.user.lastName ?? ''
    };
    this.isEditingIdentity = true;
  }

  cancelIdentityEdition(): void {
    this.isEditingIdentity = false;
    this.savingIdentity = false;
    this.identityDraft = {
      firstName: this.user?.firstName ?? '',
      lastName: this.user?.lastName ?? ''
    };
  }

  saveIdentity(): void {
    if (!this.currentUserId || !this.userPut || !this.user) {
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
      firstName: firstName,
      lastName: lastName,
      email: this.user.email ?? '',
      newEmail: this.user.email ?? '',
      preferredLanguage: this.user.preferredLanguage ?? this.translationService.getCurrentLang().toUpperCase()
    };

    this.subscriptions.add(
      this.usersApiService.putUserById(this.currentUserId, payload).subscribe({
        next: (user: UserDto) => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.user = user;
            this.userPut = {
              firstName: user.firstName,
              lastName: user.lastName,
              email: user.email,
              preferredLanguage: user.preferredLanguage,
              newEmail: user.email
            };
            this.identityDraft = {
              firstName: user.firstName ?? '',
              lastName: user.lastName ?? ''
            };
            this.isEditingIdentity = false;
            this.savingIdentity = false;
          });
          this.sharedService.emitLoginStatusChange();
          this.messageService.add('success', 'Succès', 'Profil mis à jour avec succès !');
        },
        error: (error: unknown) => {
          console.error('Error updating profile identity', error);
          commitViewUpdate(this.changeDetectorRef, () => {
            this.savingIdentity = false;
          });
          this.messageService.add('error', 'Erreur', 'La mise à jour du profil a échoué.');
        }
      })
    );
  }

  onAvatarUploadDialogVisibleChange(visible: boolean): void {
    this.displayAvatarUploadDialog = visible;
  }

  onAvatarUploaded(image: ImageDto): void {
    void image;

    if (!this.currentUserId) {
      return;
    }

    this.loadUserProfile(this.currentUserId);
    this.sharedService.emitLoginStatusChange();
    this.messageService.add('success', 'Succès', 'Avatar mis à jour avec succès !');
  }

  getAvatarUrl(): string {
    const avatarUrl: string | null = this.imagesApiService.resolveImageUrl(this.user?.avatarUrl);
    if (avatarUrl) {
      return avatarUrl;
    }

    return 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128"><circle cx="64" cy="64" r="64" fill="%23e5e7eb"/><circle cx="64" cy="46" r="22" fill="%239ca3af"/><path d="M24 110c8-18 24-28 40-28s32 10 40 28" fill="%239ca3af"/></svg>';
  }

  logout(): void {
    this.authService.logout();
    this.sharedService.emitLoginStatusChange();
    const currentLang: string = this.router.url.split('/')[1] || 'en';
    this.router.navigate([currentLang, 'home']);
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  private loadUserProfile(userId: string): void {
    this.pageState = ViewState.Loading;

    this.subscriptions.add(
      this.usersApiService.getUserById(userId).subscribe({
        next: (user: UserDto) => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.user = user;
            this.userPut = {
              firstName: user.firstName,
              lastName: user.lastName,
              email: user.email,
              preferredLanguage: user.preferredLanguage,
              newEmail: user.email
            };
            this.identityDraft = {
              firstName: user.firstName ?? '',
              lastName: user.lastName ?? ''
            };
            this.pageState = ViewState.Ready;
          });
        },
        error: (error: unknown) => {
          console.error('Error loading user profile', error);
          commitViewUpdate(this.changeDetectorRef, () => {
            this.pageState = ViewState.Error;
          });
        }
      })
    );
  }

  private updatePreferredLanguage(lang: string): void {
    if (!this.currentUserId || !this.userPut || !this.user) {
      return;
    }

    this.userPut.lastName = this.user.lastName ?? '';
    this.userPut.firstName = this.user.firstName ?? '';
    this.userPut.email = this.user.email ?? '';
    this.userPut.preferredLanguage = lang.toUpperCase();

    this.subscriptions.add(
      this.usersApiService.putUserById(this.currentUserId, this.userPut).subscribe((user: UserDto) => {
        commitViewUpdate(this.changeDetectorRef, () => {
          this.user = user;
          this.userPut = {
            firstName: user.firstName,
            lastName: user.lastName,
            email: user.email,
            preferredLanguage: user.preferredLanguage,
            newEmail: user.email
          };
        });
      })
    );
  }
}
