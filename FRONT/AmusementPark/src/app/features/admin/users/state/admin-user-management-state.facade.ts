import {
  Injectable,
  Signal,
  computed,
  DestroyRef,
  Inject,
  WritableSignal,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { UserDto } from '@app/models/users/user_dto';
import { ParkDataEditorToken } from '@app/models/users/user-admin-responses';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { TranslateService } from '@ngx-translate/core';

import {
  ADMIN_USER_MANAGEMENT_STATE_USER_ADMIN_API_SERVICE_PORT,
  ADMIN_USER_MANAGEMENT_STATE_USERS_API_SERVICE_PORT,
  AdminUserManagementStateUserAdminApiServicePort,
  AdminUserManagementStateUsersApiServicePort
} from './admin-user-management-state-data.ports';
interface AdminUserManagementViewModel {
  user: UserDto;
}

@Injectable()
export class AdminUserManagementStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminUserManagementViewModel>();
  private readonly parkDataEditorTokensState: WritableSignal<ParkDataEditorToken[]> = signal([]);
  private readonly loadingParkDataEditorTokensState: WritableSignal<boolean> = signal(false);
  private readonly revokingParkDataEditorTokenIdState: WritableSignal<string | null> = signal(null);
  private readonly revokingAllParkDataEditorTokensState: WritableSignal<boolean> = signal(false);

  public readonly state = this.screenStateStore.state;
  public readonly user: Signal<UserDto | null> = computed(() => this.screenStateStore.data()?.user ?? null);
  public readonly parkDataEditorTokens: Signal<ParkDataEditorToken[]> = this.parkDataEditorTokensState.asReadonly();
  public readonly loadingParkDataEditorTokens: Signal<boolean> = this.loadingParkDataEditorTokensState.asReadonly();
  public readonly revokingParkDataEditorTokenId: Signal<string | null> = this.revokingParkDataEditorTokenIdState.asReadonly();
  public readonly revokingAllParkDataEditorTokens: Signal<boolean> = this.revokingAllParkDataEditorTokensState.asReadonly();
  public readonly hasActiveParkDataEditorTokens: Signal<boolean> = computed(() =>
    this.parkDataEditorTokensState().some((token: ParkDataEditorToken) => token.isActive));

  constructor(
    @Inject(ADMIN_USER_MANAGEMENT_STATE_USERS_API_SERVICE_PORT)
    private readonly usersApiService: AdminUserManagementStateUsersApiServicePort,
    @Inject(ADMIN_USER_MANAGEMENT_STATE_USER_ADMIN_API_SERVICE_PORT)
    private readonly userAdminApiService: AdminUserManagementStateUserAdminApiServicePort,
    private readonly destroyRef: DestroyRef,
    private readonly toastMessageService: ToastMessageService,
    private readonly translateService: TranslateService
  ) {
  }

  loadUser(userId: string): void {
    const previousData: AdminUserManagementViewModel | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.usersApiService.getUserById(userId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (user: UserDto) => {
        this.screenStateStore.setReady({ user });
      },
      error: (error: unknown) => {
        console.error('Error while loading user', error);
        this.screenStateStore.setError('user-profile.errorMessage', previousData);
      }
    });
  }

  setUser(user: UserDto): void {
    this.screenStateStore.setReady({ user });
  }

  clearParkDataEditorTokens(): void {
    this.parkDataEditorTokensState.set([]);
    this.loadingParkDataEditorTokensState.set(false);
  }

  loadParkDataEditorTokens(userId: string): void {
    this.loadingParkDataEditorTokensState.set(true);
    this.userAdminApiService.getParkDataEditorTokens(userId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (tokens: ParkDataEditorToken[]) => {
          this.parkDataEditorTokensState.set(tokens);
          this.loadingParkDataEditorTokensState.set(false);
        },
        error: (error: unknown) => {
          console.error('Error while loading park data editor tokens', error);
          this.parkDataEditorTokensState.set([]);
          this.loadingParkDataEditorTokensState.set(false);
          this.showTokenMessage('error', 'errorTitle', 'loadError');
        }
      });
  }

  revokeParkDataEditorToken(userId: string, tokenId: string): void {
    this.revokingParkDataEditorTokenIdState.set(tokenId);
    this.userAdminApiService.revokeParkDataEditorToken(userId, tokenId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.revokingParkDataEditorTokenIdState.set(null);
          this.loadParkDataEditorTokens(userId);
          this.showTokenMessage('success', 'successTitle', 'revoked');
        },
        error: (error: unknown) => {
          console.error('Error while revoking park data editor token', error);
          this.revokingParkDataEditorTokenIdState.set(null);
          this.showTokenMessage('error', 'errorTitle', 'revokeError');
        }
      });
  }

  revokeAllParkDataEditorTokens(userId: string): void {
    this.revokingAllParkDataEditorTokensState.set(true);
    this.userAdminApiService.revokeAllParkDataEditorTokens(userId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.revokingAllParkDataEditorTokensState.set(false);
          this.loadParkDataEditorTokens(userId);
          this.showTokenMessage('success', 'successTitle', 'revokedAll');
        },
        error: (error: unknown) => {
          console.error('Error while revoking all park data editor tokens', error);
          this.revokingAllParkDataEditorTokensState.set(false);
          this.showTokenMessage('error', 'errorTitle', 'revokeError');
        }
      });
  }

  private showTokenMessage(severity: 'success' | 'error', titleKey: string, detailKey: string): void {
    this.toastMessageService.add(
      severity,
      this.translateService.instant(`admin.users.parkDataEditorTokens.${titleKey}`),
      this.translateService.instant(`admin.users.parkDataEditorTokens.${detailKey}`));
  }
}
