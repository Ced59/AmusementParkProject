import { Injectable, Signal, computed } from '@angular/core';
import { AuthApiService } from '@data-access/auth/auth-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

interface ResetPasswordPageViewModel {
  token: string;
  newPassword: string;
  confirmPassword: string;
  isSubmitted: boolean;
  message: string;
}

@Injectable()
export class ResetPasswordPageStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ResetPasswordPageViewModel>();

  public readonly state = this.screenStateStore.state;
  public readonly token: Signal<string> = computed(() => this.screenStateStore.data()?.token ?? '');
  public readonly newPassword: Signal<string> = computed(() => this.screenStateStore.data()?.newPassword ?? '');
  public readonly confirmPassword: Signal<string> = computed(() => this.screenStateStore.data()?.confirmPassword ?? '');
  public readonly isSubmitted = computed(() => this.screenStateStore.data()?.isSubmitted ?? false);
  public readonly message: Signal<string> = computed(() => this.screenStateStore.data()?.message ?? '');

  constructor(private readonly authApiService: AuthApiService) {
    this.screenStateStore.setReady({
      token: '',
      newPassword: '',
      confirmPassword: '',
      isSubmitted: false,
      message: ''
    });
  }

  initialize(token: string): void {
    this.screenStateStore.setReady({
      token,
      newPassword: '',
      confirmPassword: '',
      isSubmitted: false,
      message: ''
    });
  }

  setNewPassword(newPassword: string): void {
    const currentData: ResetPasswordPageViewModel = this.getCurrentData();
    this.screenStateStore.setReady({
      ...currentData,
      newPassword
    });
  }

  setConfirmPassword(confirmPassword: string): void {
    const currentData: ResetPasswordPageViewModel = this.getCurrentData();
    this.screenStateStore.setReady({
      ...currentData,
      confirmPassword
    });
  }

  submit(): void {
    const currentData: ResetPasswordPageViewModel = this.getCurrentData();

    if (!currentData.token || !currentData.newPassword || !currentData.confirmPassword) {
      return;
    }

    this.screenStateStore.setLoading(currentData);

    this.authApiService.resetPassword(currentData.token, currentData.newPassword, currentData.confirmPassword).subscribe({
      next: (response: { message: string }) => {
        this.screenStateStore.setReady({
          ...currentData,
          isSubmitted: true,
          message: response.message
        });
      },
      error: (error: unknown) => {
        this.screenStateStore.setReady({
          ...currentData,
          isSubmitted: true,
          message: this.resolveErrorMessage(error, 'La réinitialisation du mot de passe a échoué.')
        });
      }
    });
  }

  private getCurrentData(): ResetPasswordPageViewModel {
    return this.screenStateStore.data() ?? {
      token: '',
      newPassword: '',
      confirmPassword: '',
      isSubmitted: false,
      message: ''
    };
  }

  private resolveErrorMessage(error: unknown, fallback: string): string {
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

    return fallback;
  }
}
