import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { UserDto } from '@app/models/users/user_dto';
import { Bind } from 'primeng/bind';
import { Toolbar } from 'primeng/toolbar';
import { PrimeTemplate } from 'primeng/api';
import { Avatar } from 'primeng/avatar';
import { ButtonDirective } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { ThemeSwitcherComponent } from '../theme-switcher/theme-switcher.component';
import { AuthModalComponent } from '../login-register/auth-modal/auth-modal.component';

export interface TopbarLanguageOption {
  label: string;
  value: string;
}

@Component({
  selector: 'app-topbar-view',
  templateUrl: './topbar-view.component.html',
  styleUrls: ['./topbar.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Bind, Toolbar, PrimeTemplate, RouterLink, Avatar, ButtonDirective, ThemeSwitcherComponent, Dialog, AuthModalComponent, TranslateModule]
})
export class TopbarViewComponent {
  @Input() languages: TopbarLanguageOption[] = [];
  @Input() selectedLanguage: string | undefined;
  @Input() displayLoginModal: boolean = false;
  @Input() displayLanguageModal: boolean = false;
  @Input() isLoggedIn: boolean = false;
  @Input() userProfile: UserDto | null = null;
  @Input() userAvatarUrl: string | null = null;

  @Output() modalOpened: EventEmitter<string> = new EventEmitter<string>();
  @Output() modalClosed: EventEmitter<string> = new EventEmitter<string>();
  @Output() languageSelected: EventEmitter<string> = new EventEmitter<string>();

  openModal(modalName: string): void {
    this.modalOpened.emit(modalName);
  }

  closeModal(modalName: string): void {
    this.modalClosed.emit(modalName);
  }

  selectLanguage(lang: string): void {
    this.languageSelected.emit(lang);
  }
}
