import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';

import { LANGUAGES } from '../../../../commons/languages';
import { AppRole, APP_ROLES } from '../../../../models/users/app-role';
import { ImageCategory } from '../../../../models/images/image-category';
import { ImageDto } from '../../../../models/images/image-dto';
import { ImageOwnerType } from '../../../../models/images/image-owner-type';
import { UserDto } from '../../../../models/users/user_dto';
import { UserPut } from '../../../../models/users/user_put';
import { ViewState } from '../../../../models/shared/view-state';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { UsersApiService } from '@data-access/users/users-api.service';
import { AuthService } from '../../../../services/auth/auth.service';
import { ToastMessageService } from '../../../../services/messages/toast-message.service';
import { UserAdminApiService } from '@data-access/users/user-admin-api.service';
import { commitViewUpdate } from '../../../../utils/change-detection.utils';
import { PageStateComponent } from '../../../shared/page-state/page-state.component';
import { Bind } from 'primeng/bind';
import { ButtonDirective } from 'primeng/button';
import { NgIf, NgFor } from '@angular/common';
import { Card } from 'primeng/card';
import { Tag } from 'primeng/tag';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { OwnerImageUploadDialogComponent } from '../../../shared/owner-image-upload-dialog/owner-image-upload-dialog.component';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-admin-user-management',
    templateUrl: './admin-user-management.component.html',
    styleUrls: ['./admin-user-management.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [PageStateComponent, Bind, ButtonDirective, NgIf, Card, Tag, NgFor, FormsModule, ReactiveFormsModule, InputText, Select, OwnerImageUploadDialogComponent, TranslateModule]
})
export class AdminUserManagementComponent implements OnInit, OnDestroy {
  readonly roleOptions: AppRole[] = APP_ROLES;
  readonly languageOptions = LANGUAGES.map((language: { label: string; value: string }) => ({
    label: language.label,
    value: language.value.toUpperCase()
  }));

  readonly profileForm: FormGroup;
  readonly passwordForm: FormGroup;

  user: UserDto | null = null;
  pageState: ViewState = ViewState.Loading;
  savingProfile: boolean = false;
  savingPassword: boolean = false;
  processingRole: AppRole | null = null;
  blockingActionInProgress: boolean = false;
  displayAvatarUploadDialog: boolean = false;

  protected readonly avatarCategory = ImageCategory.AVATAR;
  protected readonly userOwnerType = ImageOwnerType.USER;
  protected readonly viewState = ViewState;

  private targetUserId: string | null = null;
  private currentUserId: string | null = null;
  private readonly subscriptions: Subscription = new Subscription();

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly usersApiService: UsersApiService,
    private readonly imagesApiService: ImagesApiService,
    private readonly userAdminApiService: UserAdminApiService,
    private readonly authService: AuthService,
    private readonly messageService: ToastMessageService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
    this.profileForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: [{ value: '', disabled: true }],
      newEmail: ['', [Validators.required, Validators.email]],
      preferredLanguage: ['EN', Validators.required]
    });

    this.passwordForm = this.fb.group({
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      newPasswordConfirm: ['', [Validators.required, Validators.minLength(8)]]
    });
  }

  ngOnInit(): void {
    this.currentUserId = this.authService.getUserIdFromToken();

    this.subscriptions.add(
      this.route.paramMap.subscribe((params) => {
        const userId: string | null = params.get('id');

        if (!userId) {
          this.navigateBack();
          return;
        }

        this.targetUserId = userId;
        this.loadUser(userId);
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  get avatarUrl(): string {
    const resolved: string | null = this.imagesApiService.resolveImageUrl(this.user?.avatarUrl);
    if (resolved) {
      return resolved;
    }

    return 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128"><circle cx="64" cy="64" r="64" fill="%23e5e7eb"/><circle cx="64" cy="46" r="22" fill="%239ca3af"/><path d="M24 110c8-18 24-28 40-28s32 10 40 28" fill="%239ca3af"/></svg>';
  }

  get isOwnProfile(): boolean {
    return this.currentUserId === this.targetUserId;
  }

  get canBlockUser(): boolean {
    return !this.isOwnProfile;
  }

  get canManagePassword(): boolean {
    return !this.isOwnProfile;
  }

  hasRole(role: AppRole): boolean {
    return this.user?.roles?.includes(role) ?? false;
  }

  saveProfile(): void {
    if (!this.targetUserId || this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    const formValue = this.profileForm.getRawValue();
    const payload: UserPut = {
      firstName: formValue.firstName,
      lastName: formValue.lastName,
      email: formValue.email,
      newEmail: formValue.newEmail,
      preferredLanguage: formValue.preferredLanguage
    };

    this.savingProfile = true;

    this.subscriptions.add(
      this.usersApiService.putUserById(this.targetUserId, payload).subscribe({
        next: (updatedUser: UserDto) => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.user = updatedUser;
            this.patchProfileForm(updatedUser);
            this.savingProfile = false;
          });
          this.messageService.add('success', 'Succès', 'Utilisateur mis à jour avec succès.');
        },
        error: (error: unknown) => {
          console.error('Error while updating user', error);
          commitViewUpdate(this.changeDetectorRef, () => {
            this.savingProfile = false;
          });
          this.messageService.add('error', 'Erreur', 'La mise à jour du profil a échoué.');
        }
      })
    );
  }

  toggleRole(role: AppRole): void {
    if (!this.targetUserId || !this.user) {
      return;
    }

    this.processingRole = role;

    const request = { role };
    const request$ = this.hasRole(role)
      ? this.userAdminApiService.removeRoleFromUser(this.targetUserId, request)
      : this.userAdminApiService.assignRoleToUser(this.targetUserId, request);

    this.subscriptions.add(
      request$.subscribe({
        next: (response) => {
          commitViewUpdate(this.changeDetectorRef, () => {
            if (this.user) {
              this.user = {
                ...this.user,
                roles: response.roles ?? this.user.roles
              };
            }
            this.processingRole = null;
          });
          this.messageService.add('success', 'Succès', `Rôle ${role} mis à jour.`);
        },
        error: (error: unknown) => {
          console.error('Error while updating role', error);
          commitViewUpdate(this.changeDetectorRef, () => {
            this.processingRole = null;
          });
          this.messageService.add('error', 'Erreur', `Impossible de mettre à jour le rôle ${role}.`);
        }
      })
    );
  }

  toggleBlockedStatus(): void {
    if (!this.targetUserId || !this.user || !this.canBlockUser) {
      return;
    }

    this.blockingActionInProgress = true;

    const request = { idUser: this.targetUserId };
    const request$ = this.user.isBlocked
      ? this.userAdminApiService.unlockUser(request)
      : this.userAdminApiService.lockUser(request);

    this.subscriptions.add(
      request$.subscribe({
        next: () => {
          commitViewUpdate(this.changeDetectorRef, () => {
            const isBlockedNow: boolean = !this.user!.isBlocked;
            this.user = {
              ...this.user!,
              isBlocked: isBlockedNow
            };
            this.blockingActionInProgress = false;
          });
          this.messageService.add(
            'success',
            'Succès',
            this.user!.isBlocked ? 'Utilisateur bloqué.' : 'Utilisateur débloqué.'
          );
        },
        error: (error: unknown) => {
          console.error('Error while updating block status', error);
          commitViewUpdate(this.changeDetectorRef, () => {
            this.blockingActionInProgress = false;
          });
          this.messageService.add('error', 'Erreur', 'Impossible de modifier le statut de blocage.');
        }
      })
    );
  }

  changePassword(): void {
    if (!this.targetUserId || !this.canManagePassword || this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    const formValue = this.passwordForm.getRawValue();

    if (formValue.newPassword !== formValue.newPasswordConfirm) {
      this.messageService.add('error', 'Erreur', 'Les mots de passe ne correspondent pas.');
      return;
    }

    this.savingPassword = true;

    this.subscriptions.add(
      this.userAdminApiService.changeUserPassword(this.targetUserId, {
        actualPassword: '',
        newPassword: formValue.newPassword,
        newPasswordConfirm: formValue.newPasswordConfirm
      }).subscribe({
        next: () => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.passwordForm.reset({ newPassword: '', newPasswordConfirm: '' });
            this.savingPassword = false;
          });
          this.messageService.add('success', 'Succès', 'Mot de passe mis à jour.');
        },
        error: (error: unknown) => {
          console.error('Error while changing password', error);
          commitViewUpdate(this.changeDetectorRef, () => {
            this.savingPassword = false;
          });
          this.messageService.add('error', 'Erreur', 'Impossible de modifier le mot de passe.');
        }
      })
    );
  }

  openAvatarDialog(): void {
    this.displayAvatarUploadDialog = true;
  }

  onAvatarUploadDialogVisibleChange(visible: boolean): void {
    this.displayAvatarUploadDialog = visible;
  }

  onAvatarUploaded(image: ImageDto): void {
    void image;

    if (!this.targetUserId) {
      return;
    }

    this.loadUser(this.targetUserId);
    this.messageService.add('success', 'Succès', 'Avatar mis à jour avec succès !');
  }

  navigateBack(): void {
    const currentLang: string = this.router.url.split('/')[1] || 'en';
    this.router.navigate(['/', currentLang, 'admin', 'users']);
  }

  private loadUser(userId: string): void {
    this.pageState = ViewState.Loading;

    this.subscriptions.add(
      this.usersApiService.getUserById(userId).subscribe({
        next: (user: UserDto) => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.user = user;
            this.patchProfileForm(user);
            this.pageState = ViewState.Ready;
          });
        },
        error: (error: unknown) => {
          console.error('Error while loading user', error);
          commitViewUpdate(this.changeDetectorRef, () => {
            this.pageState = ViewState.Error;
          });
          this.messageService.add('error', 'Erreur', 'Impossible de charger le profil utilisateur.');
        }
      })
    );
  }

  private patchProfileForm(user: UserDto): void {
    this.profileForm.patchValue({
      firstName: user.firstName ?? '',
      lastName: user.lastName ?? '',
      email: user.email ?? '',
      newEmail: user.email ?? '',
      preferredLanguage: (user.preferredLanguage ?? 'EN').toUpperCase()
    });
  }
}
