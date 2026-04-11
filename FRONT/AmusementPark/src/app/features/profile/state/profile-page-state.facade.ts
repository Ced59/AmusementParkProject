import { Injectable, Signal, computed } from '@angular/core';
import { UsersApiService } from '@data-access/users/users-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { UserDto } from '@app/models/users/user_dto';
interface ProfilePageViewModel {
  user: UserDto;
}

@Injectable()
export class ProfilePageStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ProfilePageViewModel>();

  public readonly state = this.screenStateStore.state;
  public readonly user: Signal<UserDto | null> = computed(() => this.screenStateStore.data()?.user ?? null);

  constructor(private readonly usersApiService: UsersApiService) {
  }

  loadUserProfile(userId: string): void {
    const previousData: ProfilePageViewModel | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.usersApiService.getUserById(userId).subscribe({
      next: (user: UserDto) => {
        this.screenStateStore.setReady({ user });
      },
      error: (error: unknown) => {
        console.error('Error loading user profile', error);
        this.screenStateStore.setError('user-profile.errorMessage', previousData);
      }
    });
  }

  setUser(user: UserDto): void {
    this.screenStateStore.setReady({ user });
  }

  setError(): void {
    this.screenStateStore.setError('user-profile.errorMessage', this.screenStateStore.data());
  }
}
