import { Injectable, Signal, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthApiService } from '@data-access/auth/auth-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

interface ConfirmAccountPageViewModel {
  currentLanguage: string;
  isSuccess: boolean;
  message: string;
}

@Injectable()
export class ConfirmAccountPageStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ConfirmAccountPageViewModel>();

  public readonly state = this.screenStateStore.state;
  public readonly currentLanguage: Signal<string> = computed(() => this.screenStateStore.data()?.currentLanguage ?? 'en');
  public readonly isSuccess = computed(() => this.screenStateStore.data()?.isSuccess ?? false);
  public readonly message: Signal<string> = computed(() => this.screenStateStore.data()?.message ?? '');

  constructor(private readonly authApiService: AuthApiService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  confirmEmail(token: string, currentLanguage: string): void {
    this.screenStateStore.setLoading({
      currentLanguage,
      isSuccess: false,
      message: ''
    });

    if (!token) {
      this.screenStateStore.setReady({
        currentLanguage,
        isSuccess: false,
        message: 'Le lien de confirmation est invalide.'
      });
      return;
    }

    this.authApiService.confirmEmail(token).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: { message: string }) => {
        this.screenStateStore.setReady({
          currentLanguage,
          isSuccess: true,
          message: response.message
        });
      },
      error: (error: unknown) => {
        const message: string = this.resolveErrorMessage(error);
        this.screenStateStore.setReady({
          currentLanguage,
          isSuccess: false,
          message
        });
      }
    });
  }

  private resolveErrorMessage(error: unknown): string {
    if (typeof error === 'object' && error !== null && 'error' in error) {
      const nestedError: unknown = (error as { error?: unknown }).error;

      if (typeof nestedError === 'object' && nestedError !== null) {
        if ('message' in nestedError && typeof (nestedError as { message?: unknown }).message === 'string') {
          return (nestedError as { message: string }).message;
        }

        if ('Message' in nestedError && typeof (nestedError as { Message?: unknown }).Message === 'string') {
          return (nestedError as { Message: string }).Message;
        }
      }
    }

    return 'La confirmation du compte a échoué.';
  }
}
