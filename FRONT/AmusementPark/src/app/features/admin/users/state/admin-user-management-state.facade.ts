import { Injectable, Signal, computed } from '@angular/core';
import { UsersApiService } from '@data-access/users/users-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { UserDto } from '@app/models/users/user_dto';

interface AdminUserManagementViewModel {
  user: UserDto;
}

@Injectable()
export class AdminUserManagementStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminUserManagementViewModel>();

  public readonly state = this.screenStateStore.state;
  public readonly user: Signal<UserDto | null> = computed(() => this.screenStateStore.data()?.user ?? null);

  constructor(private readonly usersApiService: UsersApiService) {
  }

  loadUser(userId: string): void {
    const previousData: AdminUserManagementViewModel | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.usersApiService.getUserById(userId).subscribe({
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
