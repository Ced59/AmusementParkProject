import { Component, OnDestroy, OnInit } from '@angular/core';
import { distinctUntilChanged, Subscription } from 'rxjs';

import { UserDto } from '../../../../models/users/user_dto';
import { UserPut } from '../../../../models/users/user_put';
import { ImageDto } from '../../../../models/images/image-dto';
import { ImageCategory } from '../../../../models/images/image-category';
import { ImageOwnerType } from '../../../../models/images/image-owner-type';
import { ApiService } from '../../../../services/api.service';
import { AuthService } from '../../../../services/auth/auth.service';
import { ToastMessageService } from '../../../../services/messages/toast-message.service';
import { ModalService } from '../../../../services/modal/modal.service';
import { SharedService } from '../../../../services/shared/shared.service';
import { TranslationService } from '../../../../services/translation.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-profile-page',
  templateUrl: './profile-page.component.html',
  styleUrl: './profile-page.component.scss',
  standalone: false
})
export class ProfilePageComponent implements OnInit, OnDestroy {
  user: UserDto | null = null;
  userPut: UserPut | null = null;
  displayAvatarUploadDialog: boolean = false;

  protected readonly avatarCategory = ImageCategory.AVATAR;
  protected readonly userOwnerType = ImageOwnerType.USER;

  private readonly subscriptions: Subscription = new Subscription();
  protected currentUserId: string | null = null;

  constructor(
    private readonly apiService: ApiService,
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly sharedService: SharedService,
    private readonly modalService: ModalService,
    private readonly translationService: TranslationService,
    private readonly messageService: ToastMessageService) {
  }

  ngOnInit(): void {
    this.currentUserId = this.authService.getUserIdFromToken();

    if (this.currentUserId) {
      this.loadUserProfile(this.currentUserId);
    }

    this.subscriptions.add(
      this.translationService.languageChanged
        .pipe(distinctUntilChanged())
        .subscribe((lang: string) => {
          this.updatePreferredLanguage(lang);
        }));
  }

  editField(field: string): void {
    if (field === 'avatar') {
      this.displayAvatarUploadDialog = true;
      return;
    }

    console.log(`Modifier le champ: ${field}`);
  }

  editPreferredLanguage(): void {
    this.modalService.openModal('languageModal');
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
    const avatarUrl: string | null = this.apiService.resolveImageUrl(this.user?.avatarUrl);
    if (avatarUrl) {
      return avatarUrl;
    }

    return 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128"><circle cx="64" cy="64" r="64" fill="%23e5e7eb"/><circle cx="64" cy="46" r="22" fill="%239ca3af"/><path d="M24 110c8-18 24-28 40-28s32 10 40 28" fill="%239ca3af"/></svg>' ;
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
    this.subscriptions.add(
      this.apiService.getUserById(userId).subscribe((user: UserDto) => {
        this.user = user;
        this.userPut = {
          firstName: user.firstName,
          lastName: user.lastName,
          email: user.email,
          preferredLanguage: user.preferredLanguage,
          newEmail: user.email
        };
      }));
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
      this.apiService.putUserById(this.currentUserId, this.userPut).subscribe((user: UserDto) => {
        this.user = user;
        this.messageService.add('success', 'Succès', 'Mise à jour de l\'utilisateur réussie !');
        this.sharedService.emitLoginStatusChange();
      }));
  }
}
