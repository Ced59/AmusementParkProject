import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { distinctUntilChanged, Subscription } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';
import { environment } from '../../../../../environments/environment';
import { UserDto } from '../../../../models/users/user_dto';
import { UserPut } from '../../../../models/users/user_put';
import { ViewState } from '../../../../models/shared/view-state';
import { ApiService } from '../../../../services/api.service';
import { AuthService } from '../../../../services/auth/auth.service';
import { SharedService } from '../../../../services/shared/shared.service';
import { ModalService } from '../../../../services/modal/modal.service';
import { TranslationService } from '../../../../services/translation.service';
import { ToastMessageService } from '../../../../services/messages/toast-message.service';
import { CurrentUserService } from '../../../../services/users/current-user.service';

@Component({
  selector: 'app-profile-page',
  templateUrl: './profile-page.component.html',
  styleUrls: ['./profile-page.component.scss']
})
export class ProfilePageComponent implements OnInit, OnDestroy {
  readonly pageState = signal<ViewState>(ViewState.Loading);
  readonly savingIdentity = signal<boolean>(false);
  readonly isEditingIdentity = signal<boolean>(false);
  readonly user = signal<UserDto | null>(null);

  identityForm: FormGroup;

  private readonly subscriptions = new Subscription();

  constructor(
    private readonly fb: FormBuilder,
    private readonly apiService: ApiService,
    private readonly authService: AuthService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly sharedService: SharedService,
    private readonly modalService: ModalService,
    private readonly translationService: TranslationService,
    private readonly messageService: ToastMessageService,
    private readonly currentUserService: CurrentUserService
  ) {
    this.identityForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.loadUserProfile();

    this.subscriptions.add(
      this.translationService.languageChanged
        .pipe(distinctUntilChanged())
        .subscribe((lang: string) => {
          const currentUser = this.user();
          if (!currentUser || currentUser.preferredLanguage?.toLowerCase() === lang.toLowerCase()) {
            return;
          }

          this.updateUser({
            firstName: currentUser.firstName ?? '',
            lastName: currentUser.lastName ?? '',
            email: currentUser.email,
            newEmail: currentUser.email,
            preferredLanguage: lang.toUpperCase()
          }, false);
        })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  startIdentityEdition(): void {
    const currentUser = this.user();
    if (!currentUser) {
      return;
    }

    this.identityForm.patchValue({
      firstName: currentUser.firstName ?? '',
      lastName: currentUser.lastName ?? ''
    });

    this.isEditingIdentity.set(true);
  }

  cancelIdentityEdition(): void {
    this.isEditingIdentity.set(false);
  }

  saveIdentity(): void {
    if (this.identityForm.invalid) {
      this.identityForm.markAllAsTouched();
      return;
    }

    const currentUser = this.user();
    if (!currentUser) {
      return;
    }

    const payload: UserPut = {
      firstName: this.identityForm.get('firstName')?.value?.trim() ?? '',
      lastName: this.identityForm.get('lastName')?.value?.trim() ?? '',
      email: currentUser.email,
      newEmail: currentUser.email,
      preferredLanguage: currentUser.preferredLanguage
    };

    this.updateUser(payload, true);
  }

  editPreferredLanguage(): void {
    this.modalService.openModal('languageModal');
  }

  logout(): void {
    this.authService.logout();
    this.currentUserService.clearCurrentUser();
    this.sharedService.emitLoginStatusChange();

    const currentLang = this.route.snapshot.paramMap.get('lang') ?? this.router.url.split('/')[1] ?? 'en';
    this.router.navigate([currentLang, 'home']);
  }

  getAvatarUrl(): string {
    const avatarUrl = this.user()?.avatarUrl;

    if (!avatarUrl) {
      return `${environment.apiImagePath}/commons/no-user-image.png`;
    }

    return avatarUrl.startsWith('http') ? avatarUrl : `${environment.apiBaseUrl}${avatarUrl}`;
  }

  protected readonly viewState = ViewState;

  private loadUserProfile(): void {
    const userId = this.authService.getUserIdFromToken();
    if (!userId) {
      this.pageState.set(ViewState.Error);
      return;
    }

    this.pageState.set(ViewState.Loading);

    this.apiService.getUserById(userId).subscribe({
      next: (user: UserDto) => {
        this.user.set(user);
        this.currentUserService.setCurrentUser(user);
        this.identityForm.patchValue({
          firstName: user.firstName ?? '',
          lastName: user.lastName ?? ''
        });
        this.pageState.set(ViewState.Ready);
      },
      error: (error: unknown) => {
        console.error('Error loading user profile', error);
        this.pageState.set(ViewState.Error);
      }
    });
  }

  private updateUser(payload: UserPut, showSuccessMessage: boolean): void {
    const currentUser = this.user();
    if (!currentUser?.id) {
      return;
    }

    this.savingIdentity.set(true);

    this.apiService.putUserById(currentUser.id, payload).subscribe({
      next: (updatedUser: UserDto) => {
        this.user.set(updatedUser);
        this.currentUserService.setCurrentUser(updatedUser);
        this.identityForm.patchValue({
          firstName: updatedUser.firstName ?? '',
          lastName: updatedUser.lastName ?? ''
        });
        this.isEditingIdentity.set(false);
        this.savingIdentity.set(false);

        if (showSuccessMessage) {
          this.messageService.add('success', 'Succès', 'Mise à jour du profil réussie.');
        }
      },
      error: (error: unknown) => {
        console.error('Error updating user profile', error);
        this.savingIdentity.set(false);
        this.messageService.add('error', 'Erreur', 'La mise à jour du profil a échoué.');
      }
    });
  }
}
