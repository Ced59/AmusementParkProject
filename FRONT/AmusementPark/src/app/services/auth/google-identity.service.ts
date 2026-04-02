import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class GoogleIdentityService {
  private isInitialized: boolean = false;
  private credentialCallback?: (response: GoogleCredentialResponse) => void;

  constructor(@Inject(PLATFORM_ID) private readonly platformId: object) {
  }

  async renderButtonAsync(
    container: HTMLElement,
    callback: (response: GoogleCredentialResponse) => void): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    await this.waitForGoogleLibraryAsync();

    this.credentialCallback = callback;
    this.initializeIfNeeded();

    container.innerHTML = '';
    window.google?.accounts.id.renderButton(container, {
      theme: 'outline',
      size: 'large',
      text: 'continue_with',
      shape: 'rectangular',
      width: 260
    });
  }

  disableAutoSelect(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    window.google?.accounts.id.disableAutoSelect();
  }

  private initializeIfNeeded(): void {
    if (this.isInitialized) {
      return;
    }

    if (!window.google?.accounts?.id) {
      throw new Error('Google Identity Services is not available.');
    }

    window.google.accounts.id.initialize({
      client_id: environment.googleClientId,
      callback: (response: GoogleCredentialResponse) => {
        if (this.credentialCallback) {
          this.credentialCallback(response);
        }
      },
      ux_mode: 'popup',
      cancel_on_tap_outside: true
    });

    this.isInitialized = true;
  }

  private async waitForGoogleLibraryAsync(): Promise<void> {
    const attempts: number = 40;
    const delayInMilliseconds: number = 250;

    for (let attempt: number = 0; attempt < attempts; attempt++) {
      if (window.google?.accounts?.id) {
        return;
      }

      await new Promise<void>((resolve: () => void) => {
        window.setTimeout(() => {
          resolve();
        }, delayInMilliseconds);
      });
    }

    throw new Error('Google Identity Services script could not be loaded.');
  }
}
