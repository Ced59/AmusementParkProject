import {Component, OnDestroy, OnInit} from '@angular/core';
import {UserDto} from "../../../../models/users/user_dto";
import {ApiService} from "../../../../services/api.service";
import {AuthService} from "../../../../services/auth/auth.service";
import {ActivatedRoute, Router} from "@angular/router";
import {environment} from "../../../../../environments/environment";
import {SharedService} from "../../../../services/shared/shared.service";
import {ModalService} from "../../../../services/modal/modal.service";
import {TranslationService} from "../../../../services/translation.service";
import {distinctUntilChanged, Subscription} from "rxjs";
import {UserPut} from "../../../../models/users/user_put";
import {MessageService} from "primeng/api";
import {ToastMessageService} from "../../../../services/messages/toast-message.service";

@Component({
    selector: 'app-profile-page',
    templateUrl: './profile-page.component.html',
    styleUrl: './profile-page.component.scss',
    standalone: false
})
export class ProfilePageComponent implements OnInit, OnDestroy{
  user: UserDto | null = null;
  private langChangeSubscription: Subscription = new Subscription();
  userPut: UserPut | null = null;

  constructor(
    private apiService: ApiService,
    private authService: AuthService,
    private route: ActivatedRoute,
    private router: Router,
    private sharedService: SharedService,
    private modalService: ModalService,
    private translationService: TranslationService,
    private messageService: ToastMessageService
  ) {}

  ngOnInit(): void {
    const userId = this.authService.getUserIdFromToken();

    if (userId) {
      this.apiService.getUserById(userId).subscribe(user => {
        this.user = user;

        this.userPut = {
          firstName: user.firstName,
          lastName: user.lastName,
          email: user.email,
          preferredLanguage: user.preferredLanguage,
          newEmail: user.email
        };
      });
    }

    this.langChangeSubscription = this.translationService.languageChanged
      .pipe(distinctUntilChanged())
      .subscribe(lang => {
      if (this.userPut && this.user) {
        this.userPut.lastName = this.user.lastName ?? '';
        this.userPut.firstName = this.user.firstName ?? '';
        this.userPut.email = this.user.email ?? '';
        this.userPut.preferredLanguage = lang.toUpperCase();

        this.apiService.putUserById(userId, this.userPut).subscribe(user => {
          this.user = user;
          this.messageService.add('success', 'Succès', 'Mise à jour de l\'utilisateur réussie !')
        });
      }
    });
  }

  editField(field: string): void {
    console.log(`Modifier le champ: ${field}`);
  }

  editPreferredLanguage(): void {
    this.modalService.openModal('languageModal');
}


  logout(): void {
    this.authService.logout();
    this.sharedService.emitLoginStatusChange();
    const currentLang = this.router.url.split('/')[1] || 'en';
    this.router.navigate([currentLang, 'home']);
  }

  ngOnDestroy() {
    if (this.langChangeSubscription) {
      this.langChangeSubscription.unsubscribe();
    }
  }

  protected readonly environment = environment;
}
