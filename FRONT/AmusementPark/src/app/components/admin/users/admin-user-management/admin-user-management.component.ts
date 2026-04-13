import { ChangeDetectionStrategy, Component, DestroyRef, Injector, OnInit, effect, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { LANGUAGES } from '../../../../commons/languages';
import { AppRole, APP_ROLES } from '../../../../models/users/app-role';
import { ImageCategory } from '../../../../models/images/image-category';
import { ImageDto } from '../../../../models/images/image-dto';
import { ImageOwnerType } from '../../../../models/images/image-owner-type';
import { UserDto } from '../../../../models/users/user_dto';
import { UserPut } from '../../../../models/users/user_put';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { UsersApiService } from '@data-access/users/users-api.service';
import { AuthService } from '../../../../services/auth/auth.service';
import { ToastMessageService } from '../../../../services/messages/toast-message.service';
import { UserAdminApiService } from '@data-access/users/user-admin-api.service';
import { PageStateComponent } from '../../../shared/page-state/page-state.component';
import { Bind } from 'primeng/bind';
import { ButtonDirective } from 'primeng/button';
import { NgIf, NgFor } from '@angular/common';
import { Card } from 'primeng/card';
import { Tag } from 'primeng/tag';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { OwnerImageUploadDialogComponent } from '../../../shared/owner-image-upload-dialog/owner-image-upload-dialog.component';
import { ImageDisplayComponent } from '../../../shared/image-display/image-display.component';
import { TranslateModule } from '@ngx-translate/core';
import { AdminUserManagementStateFacade } from '@features/admin/users/state/admin-user-management-state.facade';

@Component({
    selector: 'app-admin-user-management',
    templateUrl: './admin-user-management.component.html',
    styleUrls: ['./admin-user-management.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [AdminUserManagementStateFacade],
    imports: [PageStateComponent, Bind, ButtonDirective, NgIf, Card, Tag, NgFor, FormsModule, ReactiveFormsModule, InputText, Select, OwnerImageUploadDialogComponent, TranslateModule, ImageDisplayComponent]
})
export class AdminUserManagementComponent implements OnInit {
  readonly roleOptions: AppRole[] = APP_ROLES;
  readonly languageOptions = LANGUAGES.map((language: { label: string; value: string }) => ({
    label: language.label,
    value: language.value.toUpperCase()
  }));

  readonly profileForm: FormGroup;
  readonly passwordForm: FormGroup;

  protected readonly state = this.stateFacade.state;
  protected readonly user = this.stateFacade.user;
  savingProfile: boolean = false;
  savingPassword: boolean = false;
  processingRole: AppRole | null = null;
  blockingActionInProgress: boolean = false;
  displayAvatarUploadDialog: boolean = false;

  protected readonly avatarCategory = ImageCategory.AVATAR;
  protected readonly userOwnerType = ImageOwnerType.USER;

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly injector: Injector = inject(Injector);
  protected targetUserId: string | null = null;
  private currentUserId: string | null = null;

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly stateFacade: AdminUserManagementStateFacade,
    private readonly usersApiService: UsersApiService,
    private readonly imagesApiService: ImagesApiService,
    private readonly userAdminApiService: UserAdminApiService,
    private readonly authService: AuthService,
    private readonly messageService: ToastMessageService
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

    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const userId: string | null = params.get('id');

      if (!userId) {
        this.navigateBack();
        return;
      }

      this.targetUserId = userId;
      this.stateFacade.loadUser(userId);
    });

    effect(() => {
      const currentUser: UserDto | null = this.user();

      if (currentUser) {
        this.patchProfileForm(currentUser);
      }
    }, { injector: this.injector });
  }

  get avatarUrl(): string {
    return this.imagesApiService.resolveImageUrl(this.user()?.avatarUrl) ?? '';
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
    return this.user()?.roles?.includes(role) ?? false;
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

    this.usersApiService.putUserById(this.targetUserId, payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updatedUser: UserDto) => {
        this.stateFacade.setUser(updatedUser);
        this.patchProfileForm(updatedUser);
        this.savingProfile = false;
        this.messageService.add('success', 'Succès', 'Utilisateur mis à jour avec succès.');
      },
      error: (error: unknown) => {
        console.error('Error while updating user', error);
        this.savingProfile = false;
        this.messageService.add('error', 'Erreur', 'La mise à jour du profil a échoué.');
      }
    });
  }

  toggleRole(role: AppRole): void {
    const currentUser: UserDto | null = this.user();

    if (!this.targetUserId || !currentUser) {
      return;
    }

    this.processingRole = role;

    const request = { role };
    const request$ = this.hasRole(role)
      ? this.userAdminApiService.removeRoleFromUser(this.targetUserId, request)
      : this.userAdminApiService.assignRoleToUser(this.targetUserId, request);

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: { roles?: AppRole[] | null }) => {
        this.stateFacade.setUser({
          ...currentUser,
          roles: response.roles ?? currentUser.roles
        });
        this.processingRole = null;
        this.messageService.add('success', 'Succès', `Rôle ${role} mis à jour.`);
      },
      error: (error: unknown) => {
        console.error('Error while updating role', error);
        this.processingRole = null;
        this.messageService.add('error', 'Erreur', `Impossible de mettre à jour le rôle ${role}.`);
      }
    });
  }

  toggleBlockedStatus(): void {
    const currentUser: UserDto | null = this.user();

    if (!this.targetUserId || !currentUser || !this.canBlockUser) {
      return;
    }

    this.blockingActionInProgress = true;

    const request = { idUser: this.targetUserId };
    const request$ = currentUser.isBlocked
      ? this.userAdminApiService.unlockUser(request)
      : this.userAdminApiService.lockUser(request);

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        const updatedUser: UserDto = {
          ...currentUser,
          isBlocked: !currentUser.isBlocked
        };
        this.stateFacade.setUser(updatedUser);
        this.blockingActionInProgress = false;
        this.messageService.add('success', 'Succès', updatedUser.isBlocked ? 'Utilisateur bloqué.' : 'Utilisateur débloqué.');
      },
      error: (error: unknown) => {
        console.error('Error while updating block status', error);
        this.blockingActionInProgress = false;
        this.messageService.add('error', 'Erreur', 'Impossible de modifier le statut de blocage.');
      }
    });
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

    this.userAdminApiService.changeUserPassword(this.targetUserId, {
      actualPassword: '',
      newPassword: formValue.newPassword,
      newPasswordConfirm: formValue.newPasswordConfirm
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.passwordForm.reset({ newPassword: '', newPasswordConfirm: '' });
        this.savingPassword = false;
        this.messageService.add('success', 'Succès', 'Mot de passe mis à jour.');
      },
      error: (error: unknown) => {
        console.error('Error while changing password', error);
        this.savingPassword = false;
        this.messageService.add('error', 'Erreur', 'Impossible de modifier le mot de passe.');
      }
    });
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

    this.stateFacade.loadUser(this.targetUserId);
    this.messageService.add('success', 'Succès', 'Avatar mis à jour avec succès !');
  }

  navigateBack(): void {
    const currentLang: string = this.router.url.split('/')[1] || 'en';
    this.router.navigate(['/', currentLang, 'admin', 'users']);
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
