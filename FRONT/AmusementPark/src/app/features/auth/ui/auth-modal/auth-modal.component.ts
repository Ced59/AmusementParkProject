import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, EventEmitter, Output, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { AuthApiService } from '@data-access/auth/auth-api.service';
import { AuthService } from '@app/services/auth/auth.service';
import { GoogleIdentityService } from '@app/services/auth/google-identity.service';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { SharedService } from '@app/services/shared/shared.service';
import { AuthenticatedUserLanguageService } from '@app/services/users/authenticated-user-language.service';
import { MeasurementPreferenceService } from '@app/services/measurements/measurement-preference.service';
import { UserToken } from '@app/models/users/user_token';
import { RegisterFormComponent } from '../register-form/register-form.component';
import { LoginFormComponent } from '../login-form/login-form.component';
import { UiButtonDirective, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { extractSafeDisplayErrorMessage } from '@shared/utils/security';
import { TranslationService } from '@app/services/translation.service';

@Component({
  selector: 'app-auth-modal',
  templateUrl: './auth-modal.component.html',
  styleUrls: ['./auth-modal.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RegisterFormComponent, LoginFormComponent, TranslateModule, UiButtonDirective, UiKickerComponent, UiSurfaceDirective]
})
export class AuthModalComponent implements AfterViewInit {
  @Output() closeModal: EventEmitter<void> = new EventEmitter<void>();
  @ViewChild('googleButtonContainer', { static: true })
  private googleButtonContainer?: ElementRef<HTMLDivElement>;

  constructor(
    private readonly authApiService: AuthApiService,
    private readonly authService: AuthService,
    private readonly googleIdentityService: GoogleIdentityService,
    private readonly messageService: ToastMessageService,
    private readonly sharedService: SharedService,
    private readonly authenticatedUserLanguageService: AuthenticatedUserLanguageService,
    private readonly measurementPreferenceService: MeasurementPreferenceService,
    private readonly translationService: TranslationService,
    private readonly translateService: TranslateService,
    private readonly router: Router) {
  }

  async ngAfterViewInit(): Promise<void> {
    await this.renderGoogleButtonAsync();
  }

  async onLoginSuccess(): Promise<void> {
    this.closeModal.emit();
    await this.restoreProtectedDestinationAsync();
  }

  private async renderGoogleButtonAsync(): Promise<void> {
    if (!this.googleButtonContainer) {
      return;
    }

    try {
      await this.googleIdentityService.renderButtonAsync(
        this.googleButtonContainer.nativeElement,
        (response: GoogleCredentialResponse) => {
          void this.authenticateWithGoogleAsync(response.credential);
        });
    } catch (error: unknown) {
      console.error('Unable to render Google button.', error);
      this.messageService.add('error', this.translate('common.error', 'Error'), this.translate('auth.login.google_unavailable', 'Google sign-in is temporarily unavailable.'));
    }
  }

  private async authenticateWithGoogleAsync(idToken: string): Promise<void> {
    try {
      const result: UserToken = await firstValueFrom(this.authApiService.externalLogin(
        'google',
        idToken,
        undefined,
        this.translationService.getCurrentLang().toUpperCase(),
        this.measurementPreferenceService.getPreferredSystem()
      ));
      this.authService.setAuthenticatedSession(result);
      const returnUrl: string | null = this.resolveProtectedDestination();
      await firstValueFrom(
        this.authenticatedUserLanguageService.syncPreferredLanguageFromCurrentUser(returnUrl)
      );
      this.messageService.add('success', this.translate('common.success', 'Success'), this.translate('auth.login.google_success', 'Google sign-in succeeded.'));
      this.sharedService.emitLoginStatusChange();
      this.closeModal.emit();
      await this.restoreProtectedDestinationAsync(returnUrl);
    } catch (error: unknown) {
      const errorMessage: string = extractSafeDisplayErrorMessage(error, this.translate('common.unexpectedError', 'An unexpected error occurred.'));
      this.messageService.add('error', this.translate('common.error', 'Error'), errorMessage);
    }
  }

  private translate(key: string, fallback: string): string {
    const translatedValue: string = this.translateService.instant(key);
    return translatedValue === key ? fallback : translatedValue;
  }

  private async restoreProtectedDestinationAsync(
    destination: string | null = this.resolveProtectedDestination()
  ): Promise<void> {
    if (destination && this.router.url !== destination) {
      await this.router.navigateByUrl(destination);
    }
  }

  private resolveProtectedDestination(): string | null {
    const candidate: unknown = this.router.parseUrl(this.router.url).queryParams['returnUrl'];
    return typeof candidate === 'string'
      && candidate.startsWith('/')
      && !candidate.startsWith('//')
      ? candidate
      : null;
  }
}
