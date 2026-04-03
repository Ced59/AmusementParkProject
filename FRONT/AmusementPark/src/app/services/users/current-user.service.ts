import { Inject, Injectable, PLATFORM_ID, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ApiService } from '../api.service';
import { AuthService } from '../auth/auth.service';
import { UserDto } from '../../models/users/user_dto';

@Injectable({
  providedIn: 'root'
})
export class CurrentUserService {
  readonly currentUser = signal<UserDto | null>(null);
  readonly loading = signal<boolean>(false);

  constructor(
    private readonly apiService: ApiService,
    private readonly authService: AuthService,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {
  }

  refreshCurrentUser(): void {
    if (!isPlatformBrowser(this.platformId) || !this.authService.isLoggedIn()) {
      this.clearCurrentUser();
      return;
    }

    const userId = this.authService.getUserIdFromToken();
    if (!userId) {
      this.clearCurrentUser();
      return;
    }

    this.loading.set(true);

    this.apiService.getUserById(userId).subscribe({
      next: (user: UserDto) => {
        this.currentUser.set(user);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        console.error('Error loading current user', error);
        this.currentUser.set(null);
        this.loading.set(false);
      }
    });
  }

  setCurrentUser(user: UserDto): void {
    this.currentUser.set(user);
  }

  clearCurrentUser(): void {
    this.currentUser.set(null);
    this.loading.set(false);
  }
}
