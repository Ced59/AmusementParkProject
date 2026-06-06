import {
  Injectable,
  Signal,
  computed,
  DestroyRef,
  Inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

import {
  FORGOT_PASSWORD_PAGE_STATE_AUTH_API_SERVICE_PORT,
  ForgotPasswordPageStateAuthApiServicePort
} from './forgot-password-page-state-data.ports';
interface ForgotPasswordPageViewModel {
  email: string;
  isSubmitted: boolean;
  message: string;
}

@Injectable()
export class ForgotPasswordPageStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ForgotPasswordPageViewModel>();

  public readonly state = this.screenStateStore.state;
  public readonly email: Signal<string> = computed(() => this.screenStateStore.data()?.email ?? '');
  public readonly isSubmitted = computed(() => this.screenStateStore.data()?.isSubmitted ?? false);
  public readonly message: Signal<string> = computed(() => this.screenStateStore.data()?.message ?? '');

  constructor(@Inject(FORGOT_PASSWORD_PAGE_STATE_AUTH_API_SERVICE_PORT) private readonly authApiService: ForgotPasswordPageStateAuthApiServicePort,
    private readonly destroyRef: DestroyRef
  ) {
    this.screenStateStore.setReady({
      email: '',
      isSubmitted: false,
      message: ''
    });
  }

  setEmail(email: string): void {
    const currentData: ForgotPasswordPageViewModel = this.screenStateStore.data() ?? {
      email: '',
      isSubmitted: false,
      message: ''
    };

    this.screenStateStore.setReady({
      ...currentData,
      email
    });
  }

  submit(): void {
    const currentData: ForgotPasswordPageViewModel = this.screenStateStore.data() ?? {
      email: '',
      isSubmitted: false,
      message: ''
    };
    const email: string = currentData.email.trim();

    if (!email) {
      return;
    }

    this.screenStateStore.setLoading(currentData);

    this.authApiService.forgotPassword(email).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: { message: string }) => {
        this.screenStateStore.setReady({
          email,
          isSubmitted: true,
          message: response.message
        });
      },
      error: (error: unknown) => {
        this.screenStateStore.setReady({
          email,
          isSubmitted: true,
          message: this.resolveErrorMessage(error, 'La demande a échoué.')
        });
      }
    });
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
