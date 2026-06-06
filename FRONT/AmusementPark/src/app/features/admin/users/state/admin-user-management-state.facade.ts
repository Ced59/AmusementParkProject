import {
  Injectable,
  Signal,
  computed,
  DestroyRef,
  Inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { UserDto } from '@app/models/users/user_dto';

import {
  ADMIN_USER_MANAGEMENT_STATE_USERS_API_SERVICE_PORT,
  AdminUserManagementStateUsersApiServicePort
} from './admin-user-management-state-data.ports';
interface AdminUserManagementViewModel {
  user: UserDto;
}

@Injectable()
export class AdminUserManagementStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminUserManagementViewModel>();

  public readonly state = this.screenStateStore.state;
  public readonly user: Signal<UserDto | null> = computed(() => this.screenStateStore.data()?.user ?? null);

  constructor(@Inject(ADMIN_USER_MANAGEMENT_STATE_USERS_API_SERVICE_PORT) private readonly usersApiService: AdminUserManagementStateUsersApiServicePort,
    private readonly destroyRef: DestroyRef
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
}
