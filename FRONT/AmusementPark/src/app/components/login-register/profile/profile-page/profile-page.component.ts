import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';

import { environment } from '../../../../../environments/environment';
import { LANGUAGES } from '../../../../commons/languages';
import { UserDto } from '../../../../models/users/user_dto';
import { UserPut } from '../../../../models/users/user_put';
import { APP_ROLES, AppRole } from '../../../../models/users/app-role';
import { ApiService } from '../../../../services/api.service';
import { AuthService } from '../../../../services/auth/auth.service';
import { ToastMessageService } from '../../../../services/messages/toast-message.service';
import { TranslationService } from '../../../../services/translation.service';

@Component({
  selector: 'app-profile-page',
  templateUrl: './profile-page.component.html',
  styleUrls: ['./profile-page.component.scss']
})
export class ProfilePageComponent implements OnInit, OnDestroy {
  protected readonly environment = environment;

  readonly roleOptions: AppRole[] = APP_ROLES;
  readonly languageOptions = LANGUAGES.map((language: { label: string; value: string }) => ({
    label: language.label,
    value: language.value.toUpperCase()
  }));

  user: UserDto | null = null;
  loading = false;
  savingProfile = false;
  savingPassword = false;
  processingRole: AppRole | null = null;
  blockingActionInProgress = false;
  isAdminMode = false;
  currentUserId: string | null = null;
  targetUserId: string | null = null;

  readonly profileForm: FormGroup;
  readonly passwordForm: FormGroup;

  private routeSubscription: Subscription | null = null;

  constructor(
    private readonly fb: FormBuilder,
    private readonly apiService: ApiService,
    private readonly authService: AuthService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly messageService: ToastMessageService
  ) {
    this.profileForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
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

    this.routeSubscription = this.route.paramMap.subscribe(params => {
      const adminTargetUserId = params.get('id');
      const hasAdminRole = this.authService.hasRole('ADMIN');

      this.isAdminMode = !!adminTargetUserId && hasAdminRole;
      this.targetUserId = this.isAdminMode
        ? adminTargetUserId
        : this.currentUserId;

      if (!this.targetUserId) {
        this.navigateToHome();
        return;
      }

      this.loadUser(this.targetUserId);
    });
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
  }

  get avatarUrl(): string {
    if (this.user?.avatarUrl) {
      return `${environment.apiBaseUrl}${this.user.avatarUrl}`;
    }

    return 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="160" height="160" viewBox="0 0 160 160"><rect width="160" height="160" rx="80" fill="%23f1f5f9"/><circle cx="80" cy="62" r="28" fill="%2394a3b8"/><path d="M34 132c8-22 29-34 46-34s38 12 46 34" fill="%2394a3b8"/></svg>';
  }

  get isOwnProfile(): boolean {
    return this.currentUserId === this.targetUserId;
  }

  get canBlockUser(): boolean {
    return this.isAdminMode && !this.isOwnProfile;
  }

  get canManageRoles(): boolean {
    return this.isAdminMode;
  }

  get canManagePassword(): boolean {
    return this.isAdminMode && !this.isOwnProfile;
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

    this.apiService.putUserById(this.targetUserId, payload).subscribe({
      next: (updatedUser: UserDto) => {
        this.user = updatedUser;
        this.patchProfileForm(updatedUser);
        this.savingProfile = false;

        this.messageService.add('success', 'Succès', 'Utilisateur mis à jour avec succès.');

        if (!this.isAdminMode) {
          this.syncLanguageWithProfile(updatedUser.preferredLanguage);
        }
      },
      error: (error: unknown) => {
        console.error('Error while updating user', error);
        this.savingProfile = false;
        this.messageService.add('error', 'Erreur', 'La mise à jour du profil a échoué.');
      }
    });
  }

  toggleRole(role: AppRole): void {
    if (!this.targetUserId || !this.user) {
      return;
    }

    this.processingRole = role;

    const request = { role };
    const request$ = this.hasRole(role)
      ? this.apiService.removeRoleFromUser(this.targetUserId, request)
      : this.apiService.assignRoleToUser(this.targetUserId, request);

    request$.subscribe({
      next: (response: { roles?: AppRole[] }) => {
        if (this.user) {
          this.user = {
            ...this.user,
            roles: response.roles ?? this.user.roles
          };
        }

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
    if (!this.targetUserId || !this.user || !this.canBlockUser) {
      return;
    }

    this.blockingActionInProgress = true;

    const request = { idUser: this.targetUserId };
    const request$ = this.user.isBlocked
      ? this.apiService.unlockUser(request)
      : this.apiService.lockUser(request);

    request$.subscribe({
      next: () => {
        if (this.user) {
          this.user = {
            ...this.user,
            isBlocked: !this.user.isBlocked
          };
        }

        this.blockingActionInProgress = false;
        this.messageService.add(
          'success',
          'Succès',
          this.user?.isBlocked ? 'Utilisateur débloqué.' : 'Utilisateur bloqué.'
        );
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

    this.apiService.changeUserPassword(this.targetUserId, {
      actualPassword: '',
      newPassword: formValue.newPassword,
      newPasswordConfirm: formValue.newPasswordConfirm
    }).subscribe({
      next: () => {
        this.passwordForm.reset({
          newPassword: '',
          newPasswordConfirm: ''
        });
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

  navigateBack(): void {
    const lang = this.getCurrentLangFromUrl();

    if (this.isAdminMode) {
      this.router.navigate(['/', lang, 'admin', 'users']);
      return;
    }

    this.router.navigate(['/', lang, 'home']);
  }

  logout(): void {
    this.authService.logout();
    const currentLang = this.getCurrentLangFromUrl();
    this.router.navigate(['/', currentLang, 'home']);
  }

  private loadUser(userId: string): void {
    this.loading = true;

    this.apiService.getUserById(userId).subscribe({
      next: (user: UserDto) => {
        this.user = user;
        this.patchProfileForm(user);
        this.loading = false;
      },
      error: (error: unknown) => {
        console.error('Error while loading user profile', error);
        this.loading = false;
        this.messageService.add('error', 'Erreur', 'Impossible de charger le profil utilisateur.');
      }
    });
  }

  private patchProfileForm(user: UserDto): void {
    this.profileForm.reset({
      firstName: user.firstName ?? '',
      lastName: user.lastName ?? '',
      email: user.email ?? '',
      newEmail: user.email ?? '',
      preferredLanguage: (user.preferredLanguage ?? 'EN').toUpperCase()
    });
  }

  private syncLanguageWithProfile(preferredLanguage: string | null): void {
    const normalizedLanguage = (preferredLanguage ?? 'EN').toLowerCase();
    const currentLang = this.getCurrentLangFromUrl();

    if (normalizedLanguage === currentLang) {
      return;
    }

    this.translationService.useLang(normalizedLanguage).subscribe({
      next: () => {
        this.router.navigate(['/', normalizedLanguage, 'profile']);
      },
      error: (error: unknown) => {
        console.error('Error while switching language', error);
      }
    });
  }

  private getCurrentLangFromUrl(): string {
    return this.router.url.split('/')[1] || 'en';
  }

  private navigateToHome(): void {
    const lang = this.getCurrentLangFromUrl();
    this.router.navigate(['/', lang, 'home']);
  }
}
