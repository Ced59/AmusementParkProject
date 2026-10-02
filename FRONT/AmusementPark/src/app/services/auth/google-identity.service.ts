import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class GoogleIdentityService {
  private isInitialized: boolean = false;
  private credentialCallback?: (response: GoogleCredentialResponse) => void;
  private libraryLoadPromise: Promise<void> | null = null;

  constructor(
    @Inject(PLATFORM_ID) private readonly platformId: object,
    @Inject(DOCUMENT) private readonly document: Document
  ) {
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

  private waitForGoogleLibraryAsync(): Promise<void> {
    if (window.google?.accounts?.id) {
      return Promise.resolve();
    }

    if (this.libraryLoadPromise === null) {
      this.libraryLoadPromise = this.loadGoogleLibraryAsync().catch((error: unknown): never => {
        this.libraryLoadPromise = null;
        throw error;
      });
    }

    return this.libraryLoadPromise;
  }

  private loadGoogleLibraryAsync(): Promise<void> {
    return new Promise<void>((resolve: () => void, reject: (reason: Error) => void): void => {
      const scriptUrl: string = 'https://accounts.google.com/gsi/client';
      const existingScript: HTMLScriptElement | null = this.document.querySelector<HTMLScriptElement>(
        `script[src="${scriptUrl}"]`
      );
      const script: HTMLScriptElement = existingScript ?? this.document.createElement('script');
      const cleanUp = (): void => {
        window.clearTimeout(timeoutId);
        script.removeEventListener('load', onLoad);
        script.removeEventListener('error', onError);
      };
      const onError = (): void => {
        cleanUp();
        if (existingScript === null) {
          script.remove();
        }
        reject(new Error('Google Identity Services script could not be loaded.'));
      };
      const onLoad = (): void => {
        if (!window.google?.accounts?.id) {
          onError();
          return;
        }
        cleanUp();
        resolve();
      };
      const timeoutId: number = window.setTimeout(onError, 10_000);

      script.addEventListener('load', onLoad);
      script.addEventListener('error', onError);
      if (existingScript === null) {
        script.src = scriptUrl;
        script.async = true;
        this.document.head.appendChild(script);
      }
    });
  }
}
