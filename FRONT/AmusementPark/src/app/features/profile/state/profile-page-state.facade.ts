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
  PROFILE_PAGE_STATE_USERS_API_SERVICE_PORT,
  ProfilePageStateUsersApiServicePort
} from './profile-page-state-data.ports';
interface ProfilePageViewModel {
  user: UserDto;
}

@Injectable()
export class ProfilePageStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ProfilePageViewModel>();

  public readonly state = this.screenStateStore.state;
  public readonly user: Signal<UserDto | null> = computed(() => this.screenStateStore.data()?.user ?? null);

  constructor(@Inject(PROFILE_PAGE_STATE_USERS_API_SERVICE_PORT) private readonly usersApiService: ProfilePageStateUsersApiServicePort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  loadUserProfile(userId: string): void {
    const previousData: ProfilePageViewModel | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.usersApiService.getUserById(userId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
