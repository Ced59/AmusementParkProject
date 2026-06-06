import {
  Injectable,
  Signal,
  computed,
  signal,
  DestroyRef,
  Inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Pagination } from '@app/models/shared/pagination';
import { UserDto } from '@app/models/users/user_dto';
import { UsersApiResponse } from '@app/models/users/users_api_response';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

import {
  ADMIN_USERS_STATE_USERS_API_SERVICE_PORT,
  AdminUsersStateUsersApiServicePort
} from './admin-users-state-data.ports';
interface AdminUsersViewModel {
  users: UserDto[];
  pagination: Pagination | null;
  totalRecords: number;
  currentPage: number;
  pageSize: number;
}

@Injectable()
export class AdminUsersStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminUsersViewModel>();
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(10);

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly users: Signal<UserDto[]> = computed(() => this.screenStateStore.data()?.users ?? []);
  public readonly pagination: Signal<Pagination | null> = computed(() => this.screenStateStore.data()?.pagination ?? null);
  public readonly totalRecords = computed(() => this.screenStateStore.data()?.totalRecords ?? 0);
  public readonly currentPage = this.currentPageSignal.asReadonly();
  public readonly pageSize = this.pageSizeSignal.asReadonly();

  constructor(@Inject(ADMIN_USERS_STATE_USERS_API_SERVICE_PORT) private readonly usersApiService: AdminUsersStateUsersApiServicePort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  loadUsers(page: number = this.currentPageSignal(), size: number = this.pageSizeSignal()): void {
    const previousData: AdminUsersViewModel | undefined = this.screenStateStore.data();

    this.currentPageSignal.set(page);
    this.pageSizeSignal.set(size);
    this.screenStateStore.setLoading(previousData);

    this.usersApiService.getUsers(page, size).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: UsersApiResponse) => {
        const users: UserDto[] = response.data ?? [];
        const pagination: Pagination | null = response.pagination ?? null;
        const viewModel: AdminUsersViewModel = {
          users,
          pagination,
          totalRecords: pagination?.totalItems ?? users.length,
          pageSize: pagination?.itemsPerPage ?? size,
          currentPage: pagination?.currentPage ?? page
        };

        this.currentPageSignal.set(viewModel.currentPage);
        this.pageSizeSignal.set(viewModel.pageSize);

        if (viewModel.totalRecords === 0) {
          this.screenStateStore.setEmpty(viewModel);
          return;
        }

        this.screenStateStore.setReady(viewModel);
      },
      error: (error: unknown) => {
        console.error('Error loading users', error);
        this.screenStateStore.setError('admin.users.loadError', previousData);
      }
    });
  }
}
